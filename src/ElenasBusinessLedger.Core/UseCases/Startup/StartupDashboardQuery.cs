using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Time;

namespace ElenasBusinessLedger.Core.UseCases.Startup;

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

    public async Task<OperationResult<StartupDashboardResult>> ExecuteAsync(
        DateOnly selectedMonth,
        string? selectedStudentShortName,
        CancellationToken cancellationToken)
    {
        var from = selectedMonth.AddMonths(-1).WithDay(1);
        var to = selectedMonth.AddMonths(2).WithDay(1).AddDays(-1);
        var students = await studentRepository.ListActiveStudentsAsync(cancellationToken);
        var calendarLessons = await calendarGateway.ListLessonsAsync(from, to, cancellationToken);
        var activeStudentShortNames = students
            .GroupBy(student => student.Student.ShortName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Student.ShortName,
                StringComparer.OrdinalIgnoreCase);
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

            if (activeStudentShortNames.TryGetValue(local?.StudentShortName ?? calendar.StudentShortName, out var shortName))
            {
                known.Add((calendar with { StudentShortName = shortName }, local));
            }
        }

        var knownCalendarLessons = known.Select(match => match.Calendar).ToArray();
        var currentStudent = selectedStudentShortName is not null &&
                             activeStudentShortNames.TryGetValue(selectedStudentShortName, out var canonicalSelectedStudent)
            ? canonicalSelectedStudent
            : PickCurrentStudent(knownCalendarLessons, students);

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
            if (!MatchesCurrentStudent(lesson.StudentShortName, currentStudent))
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
            .Where(lesson => statusByLessonId.ContainsKey(lesson.Id))
            .Where(lesson => !representedLocalLessonIds.Contains(lesson.Id))
            .Where(lesson => MatchesCurrentStudent(lesson.StudentShortName, currentStudent)))
        {
            var invoiceStatus = statusByLessonId.GetValueOrDefault(localLesson.Id);
            dashboardLessons.Add(CreateDashboardLesson(localLesson, invoiceStatus));
        }

        var lessons = dashboardLessons
            .Where(lesson => MatchesCurrentStudent(lesson.StudentShortName, currentStudent))
            .OrderBy(lesson => lesson.StartsAt)
            .ToArray();

        var selectedStudent = students.FirstOrDefault(student =>
            string.Equals(student.Student.ShortName, currentStudent, StringComparison.OrdinalIgnoreCase));
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
                lessonDefaults));
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

        if (record != localLesson)
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
            record.HasKnownStartTime);
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
            string.Equals(student.Student.ShortName, calendarLesson.StudentShortName, StringComparison.OrdinalIgnoreCase));
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

    private string? PickCurrentStudent(IReadOnlyList<CalendarLesson> calendarLessons, IReadOnlyList<StudentRecord> students)
    {
        var now = clock.Now;
        return calendarLessons
            .Where(lesson => lesson.StartsAt.Date == now.Date)
            .OrderBy(lesson => Math.Abs((lesson.StartsAt - now).Ticks))
            .Select(lesson => lesson.StudentShortName)
            .FirstOrDefault() ??
            students.FirstOrDefault()?.Student.ShortName;
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
            .Select(lesson => new DashboardStudentNavigationEntry(lesson.StudentShortName, lesson.StartsAt))
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
                    HasCalendarLesson: false)))
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

    private static bool MatchesCurrentStudent(string studentShortName, string? currentStudent) =>
        currentStudent is null ||
        string.Equals(studentShortName, currentStudent, StringComparison.OrdinalIgnoreCase);

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

internal static class DateOnlyExtensions
{
    public static DateOnly WithDay(this DateOnly date, int day) => new(date.Year, date.Month, day);
}
