using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Time;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Startup;

// Loads the selected student's dashboard by coordinating calendar reconciliation and a read-only projection.
public sealed class LoadDashboardUseCase
{
    private readonly IStudentRepository studentRepository;
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IClock clock;
    private readonly ILessonRepository lessonRepository;
    private readonly IPlanCatalogRepository? planCatalogRepository;
    private readonly DashboardLessonReconciler reconciler;
    private readonly DashboardProjection projection;

    public LoadDashboardUseCase(
        IStudentRepository studentRepository,
        IGoogleCalendarGateway calendarGateway,
        IInvoiceRepository invoiceRepository,
        IClock clock,
        ILessonRepository lessonRepository,
        TimeZoneInfo? lessonTimeZone = null,
        IPlanCatalogRepository? planCatalogRepository = null)
    {
        this.studentRepository = studentRepository;
        this.calendarGateway = calendarGateway;
        this.invoiceRepository = invoiceRepository;
        this.clock = clock;
        this.lessonRepository = lessonRepository;
        this.planCatalogRepository = planCatalogRepository;
        reconciler = new DashboardLessonReconciler(lessonRepository);
        projection = new DashboardProjection(lessonTimeZone ?? TimeZoneInfo.Local);
    }

    // Reconciles calendar appointments with local billing records and builds the dashboard for the selected student.
    public async Task<OperationResult<StartupDashboardResult>> ExecuteAsync(
        DateOnly selectedMonth,
        string? selectedStudentShortName,
        CancellationToken cancellationToken,
        int? selectedStudentId = null)
    {
        var from = selectedMonth.AddMonths(-1).WithDay(1);
        var to = selectedMonth.AddMonths(2).WithDay(1).AddDays(-1);
        var students = await studentRepository.ListActiveStudentsAsync(cancellationToken);
        var calendarLessons = await calendarGateway.ListLessonsAsync(from, to, cancellationToken);
        var studentsById = students.Where(student => student.Id > 0).ToDictionary(student => student.Id);
        var uniqueNames = students.GroupBy(student => StudentShortNameKey.From(student.Student.ShortName))
            .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single());
        var eventIds = calendarLessons
            .SelectMany(lesson => lesson.LegacyEventId is { } rawId ? [lesson.Id, rawId] : new[] { lesson.Id })
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var localLinks = await lessonRepository.ListByGoogleCalendarEventIdsAsync(eventIds, cancellationToken);
        var byEventId = localLinks.ToDictionary(lesson => lesson.GoogleCalendarEventId!, StringComparer.Ordinal);
        var known = new List<(CalendarLesson Calendar, LessonRecord? Local)>();
        foreach (var calendar in calendarLessons)
        {
            var local = byEventId.GetValueOrDefault(calendar.Id);
            if (local is null && calendar.LegacyEventId is { } legacyId &&
                byEventId.TryGetValue(legacyId, out var legacy))
            {
                // Earlier releases stored only the external ID. Require the observed owner and time before adopting it.
                if (!string.Equals(legacy.StudentShortName, calendar.StudentShortName, StringComparison.OrdinalIgnoreCase) ||
                    legacy.StartsAt != calendar.StartsAt || legacy.HasKnownStartTime != calendar.HasKnownStartTime)
                {
                    continue;
                }

                local = legacy;
            }

            var owner = local is { StudentId: > 0 }
                ? studentsById.GetValueOrDefault(local.StudentId)
                    ?? students.FirstOrDefault(student => student.Id <= 0 && student.Student.ShortName == local.StudentShortName)
                : uniqueNames.GetValueOrDefault(StudentShortNameKey.From(local?.StudentShortName ?? calendar.StudentShortName));
            if (owner is not null)
            {
                known.Add((calendar with { StudentShortName = owner.Student.ShortName, StudentId = owner.Id > 0 ? owner.Id : null }, local));
            }
        }

        var knownCalendarLessons = known.Select(match => match.Calendar).ToArray();
        var selectedStudent = (selectedStudentId is { } selectedId
                ? studentsById.GetValueOrDefault(selectedId)
                : selectedStudentShortName is null ? null
                    : uniqueNames.GetValueOrDefault(StudentShortNameKey.From(selectedStudentShortName)))
            ?? PickCurrentStudent(knownCalendarLessons, students);

        var lessonStatuses = await invoiceRepository.ListLessonStatusesAsync(
            from, to, cancellationToken, localLinks.Select(lesson => lesson.Id).ToArray());
        var statusByLessonId = lessonStatuses
            .Where(status => status.LessonId > 0)
            .GroupBy(status => status.LessonId)
            .ToDictionary(group => group.Key, group => group.First());
        var localLessons = await lessonRepository.ListForWindowAsync(from, to, cancellationToken);
        var lessonSources = new List<(LessonRecord Record, InvoiceLessonStatus? InvoiceStatus)>();
        var representedLocalLessonIds = new HashSet<int>();
        foreach (var (lesson, localLesson) in known.OrderBy(match => match.Calendar.StartsAt))
        {
            if (!MatchesCurrentStudent(lesson.StudentShortName, lesson.StudentId, selectedStudent))
            {
                continue;
            }

            var invoiceStatus = localLesson is null
                ? null
                : statusByLessonId.GetValueOrDefault(localLesson.Id);
            var reconciled = await reconciler.ReconcileAsync(lesson, localLesson, invoiceStatus is not null, students, cancellationToken);
            lessonSources.Add((reconciled, invoiceStatus));

            if (localLesson is not null)
            {
                representedLocalLessonIds.Add(localLesson.Id);
            }
        }

        // An omitted event can belong to another calendar, be outside the date range, or have a changed title.
        // Only an explicit lesson deletion removes a billing record.

        foreach (var localLesson in localLessons
            .Where(lesson => statusByLessonId.ContainsKey(lesson.Id) || lesson.PendingCalendarMutation is not null)
            .Where(lesson => !representedLocalLessonIds.Contains(lesson.Id))
            .Where(lesson => MatchesCurrentStudent(lesson.StudentShortName, lesson.StudentId, selectedStudent)))
        {
            var invoiceStatus = statusByLessonId.GetValueOrDefault(localLesson.Id);
            lessonSources.Add((localLesson, invoiceStatus));
        }

        var selectedLessons = lessonSources.Where(source =>
            MatchesCurrentStudent(source.Record.StudentShortName, source.Record.StudentId, selectedStudent));
        var invoices = selectedStudent is null
            ? []
            : await invoiceRepository.ListInvoicesForStudentAsync(selectedStudent.Id, cancellationToken);
        var studentNavigationEntries = projection.CreateStudentNavigationEntries(selectedMonth, knownCalendarLessons, students);
        var planOptions = await CreatePlanOptionsAsync(students, cancellationToken);

        return OperationResult<StartupDashboardResult>.Success(
            DashboardProjection.Create(selectedStudent, students, selectedLessons, invoices, studentNavigationEntries, planOptions));
    }

    private async Task<IReadOnlyList<DashboardPlanOption>> CreatePlanOptionsAsync(
        IReadOnlyList<StudentRecord> students,
        CancellationToken cancellationToken)
    {
        if (planCatalogRepository is not null)
        {
            var assignablePlans = await planCatalogRepository.ListAssignableAsync(cancellationToken);
            return assignablePlans
                .OrderBy(plan => plan.Name, StringComparer.OrdinalIgnoreCase)
                .Select(plan => new DashboardPlanOption(plan.Id, plan.Name))
                .ToArray();
        }

        return students
            .SelectMany(student => student.PlanPeriods)
            .Select(period => new DashboardPlanOption(period.Plan.PlanCatalogId, period.Plan.Name))
            .GroupBy(option => option.PlanCatalogId)
            .Select(group => group.First())
            .OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private StudentRecord? PickCurrentStudent(IReadOnlyList<CalendarLesson> calendarLessons, IReadOnlyList<StudentRecord> students)
    {
        var now = clock.Now;
        var nearest = calendarLessons.Where(lesson => lesson.StartsAt.Date == now.Date)
            .MinBy(lesson => Math.Abs((lesson.StartsAt - now).Ticks));
        return nearest is null ? students.FirstOrDefault() :
            students.FirstOrDefault(student => MatchesCurrentStudent(nearest.StudentShortName, nearest.StudentId, student));
    }

    private static bool MatchesCurrentStudent(string shortName, int? studentId, StudentRecord? selected) =>
        selected is null || (studentId is > 0 && selected.Id > 0
            ? studentId == selected.Id
            : string.Equals(shortName, selected.Student.ShortName, StringComparison.OrdinalIgnoreCase));
}

// Supplies the day replacement used to calculate month boundaries for the dashboard's calendar window.
internal static class DateOnlyExtensions
{
    public static DateOnly WithDay(this DateOnly date, int day) => new(date.Year, date.Month, day);
}
