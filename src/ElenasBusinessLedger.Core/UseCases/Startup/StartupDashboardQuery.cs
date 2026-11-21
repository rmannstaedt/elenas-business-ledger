using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Time;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Startup;

// Reconciles calendar observations with the ledger and prepares the selected student's dashboard data.
public sealed class StartupDashboardQuery
{
    private readonly IStudentRepository studentRepository;
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IClock clock;
    private readonly ILessonRepository lessonRepository;
    private readonly IPlanCatalogRepository? planCatalogRepository;
    private readonly TimeZoneInfo lessonTimeZone;

    public StartupDashboardQuery(
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
        this.lessonTimeZone = lessonTimeZone ?? TimeZoneInfo.Local;
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
        var currentStudent = selectedStudent?.Student.ShortName;

        var lessonStatuses = await invoiceRepository.ListLessonStatusesAsync(
            from, to, cancellationToken, localLinks.Select(lesson => lesson.Id).ToArray());
        var statusByLessonId = lessonStatuses
            .Where(status => status.LessonId > 0)
            .GroupBy(status => status.LessonId)
            .ToDictionary(group => group.Key, group => group.First());
        var localLessons = await lessonRepository.ListForWindowAsync(from, to, cancellationToken);
        var dashboardLessons = new List<DashboardLesson>();
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
            var dashboardLesson = await ReconcileCalendarLessonAsync(lesson, localLesson, invoiceStatus, students, cancellationToken);
            dashboardLessons.Add(dashboardLesson);

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
            dashboardLessons.Add(CreateDashboardLesson(localLesson, invoiceStatus));
        }

        var lessons = dashboardLessons
            .Where(lesson => MatchesCurrentStudent(lesson.StudentShortName, lesson.StudentId, selectedStudent))
            .OrderBy(lesson => lesson.StartsAt)
            .ToArray();

        var invoices = selectedStudent is null
            ? []
            : await invoiceRepository.ListInvoicesForStudentAsync(selectedStudent.Id, cancellationToken);

        var studentShortNames = students
            .Select(student => student.Student.ShortName)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var studentNavigationEntries = CreateStudentNavigationEntries(selectedMonth, knownCalendarLessons, students);
        var planOptions = await CreatePlanOptionsAsync(students, cancellationToken);
        var lessonDefaults = selectedStudent is null
            ? null
            : new DashboardStudentLessonDefaults(selectedStudent.Student.ShortName, selectedStudent.Schedule, selectedStudent.PlanPeriods);

        return OperationResult<StartupDashboardResult>.Success(
            new StartupDashboardResult(
                currentStudent,
                studentShortNames,
                lessons,
                invoices,
                selectedStudent?.Schedule.Count > 0,
                studentNavigationEntries,
                planOptions,
                lessonDefaults,
                selectedStudent is { Id: > 0 } ? selectedStudent.Id : null,
                students.Select(student => new StudentChoice(student.Id, student.Student.ShortName)).ToArray()));
    }

    private async Task<DashboardLesson> ReconcileCalendarLessonAsync(
        CalendarLesson calendarLesson,
        LessonRecord? localLesson,
        InvoiceLessonStatus? invoiceStatus,
        IReadOnlyList<StudentRecord> students,
        CancellationToken cancellationToken)
    {
        if (localLesson is not null && invoiceStatus is not null)
        {
            return CreateDashboardLesson(localLesson, invoiceStatus);
        }

        var record = localLesson is null
            ? CreateLocalLesson(calendarLesson, students)
            : localLesson with
            {
                StartsAt = calendarLesson.StartsAt,
                HasKnownStartTime = calendarLesson.HasKnownStartTime,
                State = calendarLesson.State,
                GoogleCalendarEventId = calendarLesson.Id,
                InvoiceNumber = null
            };

        if (record.PlanCatalogId > 0 && record != localLesson)
        {
            record = await lessonRepository.SaveAsync(record, cancellationToken);
        }

        return CreateDashboardLesson(record, invoiceStatus);
    }

    private static DashboardLesson CreateDashboardLesson(
        LessonRecord record,
        InvoiceLessonStatus? invoiceStatus)
    {
        var invoiceNumber = invoiceStatus?.InvoiceNumber;
        var isInvoiced = invoiceNumber is not null;

        return new(
            record.GoogleCalendarEventId ?? record.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            record.StudentShortName,
            record.StartsAt,
            isInvoiced ? LessonState.Completed : record.State,
            isInvoiced,
            invoiceStatus?.PaymentStatus,
            invoiceNumber,
            record.PlanCatalogId,
            record.PlanName,
            record.Id,
            record.HasKnownStartTime,
            record.StudentId > 0 ? record.StudentId : null,
            record.PendingCalendarMutation is not null,
            record.PlanCatalogId <= 0);
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

    private static LessonRecord CreateLocalLesson(
        CalendarLesson calendarLesson,
        IReadOnlyList<StudentRecord> students)
    {
        var student = students.FirstOrDefault(student =>
            calendarLesson.StudentId is { } studentId ? student.Id == studentId :
                string.Equals(student.Student.ShortName, calendarLesson.StudentShortName, StringComparison.Ordinal));
        var lessonDate = DateOnly.FromDateTime(calendarLesson.StartsAt.DateTime);
        var plan = student is null
            ? null
            : StudentPlanPeriod.GetActivePlan(student.PlanPeriods, lessonDate);

        return new LessonRecord(
            Id: 0,
            StudentId: student?.Id ?? 0,
            StudentShortName: calendarLesson.StudentShortName,
            PlanCatalogId: plan?.PlanCatalogId ?? 0,
            PlanName: plan?.Name ?? string.Empty,
            StartsAt: calendarLesson.StartsAt,
            DurationMinutes: plan?.DurationMinutes ?? 60,
            State: calendarLesson.State,
            GoogleCalendarEventId: calendarLesson.Id,
            InvoiceNumber: null,
            HasKnownStartTime: calendarLesson.HasKnownStartTime);
    }

    private StudentRecord? PickCurrentStudent(IReadOnlyList<CalendarLesson> calendarLessons, IReadOnlyList<StudentRecord> students)
    {
        var now = clock.Now;
        var nearest = calendarLessons.Where(lesson => lesson.StartsAt.Date == now.Date)
            .MinBy(lesson => Math.Abs((lesson.StartsAt - now).Ticks));
        return nearest is null ? students.FirstOrDefault() :
            students.FirstOrDefault(student => MatchesCurrentStudent(nearest.StudentShortName, nearest.StudentId, student));
    }

    private IReadOnlyList<DashboardStudentNavigationEntry> CreateStudentNavigationEntries(
        DateOnly selectedMonth,
        IReadOnlyList<CalendarLesson> calendarLessons,
        IReadOnlyList<StudentRecord> students)
    {
        var fallbackFrom = selectedMonth.WithDay(1);
        var fallbackTo = fallbackFrom.AddMonths(1).AddDays(-1);
        var navigationFrom = selectedMonth.AddMonths(-1).WithDay(1);
        var navigationTo = selectedMonth.AddMonths(2).WithDay(1).AddDays(-1);
        var activeStudentShortNames = students
            .Select(student => student.Student.ShortName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var scheduledEntries = calendarLessons
            .Where(lesson => activeStudentShortNames.Contains(lesson.StudentShortName))
            .Where(lesson => IsInDateWindow(lesson.StartsAt, navigationFrom, navigationTo))
            .Select(lesson => new DashboardStudentNavigationEntry(lesson.StudentShortName, lesson.StartsAt, StudentId: lesson.StudentId))
            .OrderBy(entry => entry.StartsAt)
            .ThenBy(entry => entry.StudentShortName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (scheduledEntries.Length > 0)
        {
            return scheduledEntries;
        }

        var defaultEntries = students
            .SelectMany(student => CreateScheduledStarts(student.Schedule, fallbackFrom, fallbackTo)
                .Select(startsAt => new DashboardStudentNavigationEntry(
                    student.Student.ShortName,
                    startsAt,
                    HasCalendarLesson: false,
                    StudentId: student.Id > 0 ? student.Id : null)))
            .ToArray();

        return OrderNavigationEntries(defaultEntries);
    }

    private static IReadOnlyList<DashboardStudentNavigationEntry> OrderNavigationEntries(
        IEnumerable<DashboardStudentNavigationEntry> entries) =>
        entries
            .OrderBy(entry => entry.StartsAt)
            .ThenBy(entry => entry.StudentShortName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static bool IsInDateWindow(DateTimeOffset startsAt, DateOnly from, DateOnly to)
    {
        var date = DateOnly.FromDateTime(startsAt.DateTime);
        return date >= from && date <= to;
    }

    private static bool MatchesCurrentStudent(string shortName, int? studentId, StudentRecord? selected) =>
        selected is null || (studentId is > 0 && selected.Id > 0
            ? studentId == selected.Id
            : string.Equals(shortName, selected.Student.ShortName, StringComparison.OrdinalIgnoreCase));

    private IEnumerable<DateTimeOffset> CreateScheduledStarts(
        IReadOnlyList<ElenasBusinessLedger.Core.Students.StudentScheduleEntry> schedule,
        DateOnly from,
        DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var entry in schedule)
            {
                if (entry.Weekday != date.DayOfWeek)
                {
                    continue;
                }

                var localDateTime = date.ToDateTime(entry.StartsAt);
                yield return new DateTimeOffset(localDateTime, lessonTimeZone.GetUtcOffset(localDateTime));
            }
        }
    }
}

// Supplies the day replacement used to calculate month boundaries for the dashboard's calendar window.
internal static class DateOnlyExtensions
{
    public static DateOnly WithDay(this DateOnly date, int day) => new(date.Year, date.Month, day);
}
