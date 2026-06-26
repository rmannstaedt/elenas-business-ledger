using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.Time;

namespace MySmallBusinessLedger.Core.UseCases.Startup;

public sealed class StartupDashboardQuery
{
    private readonly IStudentRepository studentRepository;
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IClock clock;
    private readonly ILessonRepository? lessonRepository;
    private readonly IPlanCatalogRepository? planCatalogRepository;
    private readonly TimeZoneInfo lessonTimeZone;

    public StartupDashboardQuery(
        IStudentRepository studentRepository,
        IGoogleCalendarGateway calendarGateway,
        IInvoiceRepository invoiceRepository,
        IClock clock,
        TimeZoneInfo? lessonTimeZone = null,
        ILessonRepository? lessonRepository = null,
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
        var currentStudent = selectedStudentShortName ?? PickCurrentStudent(calendarLessons, students);

        var lessonStatuses = await invoiceRepository.ListLessonStatusesAsync(from, to, cancellationToken);
        var statusLookup = lessonStatuses.ToDictionary(
            status => (status.StudentShortName, status.LessonDateTime),
            status => status);
        var localLessons = lessonRepository is null
            ? []
            : await lessonRepository.ListForWindowAsync(from, to, cancellationToken);
        var localByCalendarId = localLessons
            .Where(lesson => !string.IsNullOrWhiteSpace(lesson.GoogleCalendarEventId))
            .ToDictionary(lesson => lesson.GoogleCalendarEventId!, StringComparer.OrdinalIgnoreCase);

        var dashboardLessons = new List<DashboardLesson>();
        var representedLocalLessonIds = new HashSet<int>();
        foreach (var lesson in calendarLessons.OrderBy(lesson => lesson.StartsAt))
        {
            if (currentStudent is not null && lesson.StudentShortName != currentStudent)
            {
                continue;
            }

            var invoiceStatus = statusLookup.GetValueOrDefault((lesson.StudentShortName, lesson.StartsAt));
            var localLesson = localByCalendarId.GetValueOrDefault(lesson.Id);
            var dashboardLesson = lessonRepository is null
                ? CreateLegacyDashboardLesson(lesson, invoiceStatus)
                : await ReconcileCalendarLessonAsync(lesson, localLesson, invoiceStatus, students, cancellationToken);
            dashboardLessons.Add(dashboardLesson);

            if (localLesson is not null)
            {
                representedLocalLessonIds.Add(localLesson.Id);
            }
        }

        if (lessonRepository is not null)
        {
            var currentCalendarLessonIds = calendarLessons
                .Select(lesson => lesson.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingUninvoicedLocalLessonIds = localLessons
                .Where(lesson => lesson.InvoiceNumber is null)
                .Where(lesson => !string.IsNullOrWhiteSpace(lesson.GoogleCalendarEventId))
                .Where(lesson => !currentCalendarLessonIds.Contains(lesson.GoogleCalendarEventId!))
                .Select(lesson => lesson.Id)
                .ToArray();
            await lessonRepository.DeleteUninvoicedAsync(missingUninvoicedLocalLessonIds, cancellationToken);

            foreach (var localLesson in localLessons
                .Where(lesson => lesson.InvoiceNumber is not null)
                .Where(lesson => !representedLocalLessonIds.Contains(lesson.Id))
                .Where(lesson => currentStudent is null || lesson.StudentShortName == currentStudent))
            {
                var invoiceStatus = statusLookup.GetValueOrDefault((localLesson.StudentShortName, localLesson.StartsAt));
                dashboardLessons.Add(CreateDashboardLesson(localLesson, invoiceStatus));
            }
        }

        var lessons = dashboardLessons
            .Where(lesson => currentStudent is null || lesson.StudentShortName == currentStudent)
            .OrderBy(lesson => lesson.StartsAt)
            .ToArray();

        var invoices = currentStudent is null
            ? []
            : await invoiceRepository.ListInvoicesForStudentAsync(currentStudent, cancellationToken);

        var studentShortNames = students
            .Select(student => student.Student.ShortName)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var studentNavigationEntries = CreateStudentNavigationEntries(selectedMonth, calendarLessons, students);
        var planOptions = await CreatePlanOptionsAsync(students, cancellationToken);

        return OperationResult<StartupDashboardResult>.Success(
            new StartupDashboardResult(
                currentStudent,
                studentShortNames,
                lessons,
                invoices,
                CurrentStudentHasSchedule(students, currentStudent),
                studentNavigationEntries,
                planOptions));
    }

    private static DashboardLesson CreateLegacyDashboardLesson(
        CalendarLesson lesson,
        InvoiceLessonStatus? invoiceStatus)
    {
        if (invoiceStatus is null)
        {
            return new DashboardLesson(
                lesson.Id,
                lesson.StudentShortName,
                lesson.StartsAt,
                lesson.State,
                IsInvoiced: false,
                PaymentStatus: null);
        }

        return new DashboardLesson(
            lesson.Id,
            lesson.StudentShortName,
            lesson.StartsAt,
            LessonState.Completed,
            IsInvoiced: true,
            invoiceStatus.PaymentStatus,
            invoiceStatus.InvoiceNumber);
    }

    private async Task<DashboardLesson> ReconcileCalendarLessonAsync(
        CalendarLesson calendarLesson,
        LessonRecord? localLesson,
        InvoiceLessonStatus? invoiceStatus,
        IReadOnlyList<StudentRecord> students,
        CancellationToken cancellationToken)
    {
        var record = localLesson is null
            ? CreateLocalLesson(calendarLesson, invoiceStatus, students)
            : localLesson.InvoiceNumber is null
                ? localLesson with
                {
                    StartsAt = calendarLesson.StartsAt,
                    State = calendarLesson.State
                }
                : localLesson;

        record = await lessonRepository!.SaveAsync(record, cancellationToken);

        return CreateDashboardLesson(record, invoiceStatus);
    }

    private static DashboardLesson CreateDashboardLesson(
        LessonRecord record,
        InvoiceLessonStatus? invoiceStatus) =>
        new(
            record.GoogleCalendarEventId ?? record.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            record.StudentShortName,
            record.StartsAt,
            record.InvoiceNumber is null ? record.State : LessonState.Completed,
            IsInvoiced: record.InvoiceNumber is not null,
            invoiceStatus?.PaymentStatus,
            record.InvoiceNumber,
            record.PlanCatalogId,
            record.PlanName,
            record.Id);

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
        InvoiceLessonStatus? invoiceStatus,
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
            InvoiceNumber: invoiceStatus?.InvoiceNumber);
    }

    private static bool CurrentStudentHasSchedule(IReadOnlyList<StudentRecord> students, string? currentStudent) =>
        currentStudent is not null &&
        students
            .FirstOrDefault(student => string.Equals(
                student.Student.ShortName,
                currentStudent,
                StringComparison.OrdinalIgnoreCase))
            ?.Schedule.Count > 0;

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

    private IEnumerable<DateTimeOffset> CreateScheduledStarts(
        IReadOnlyList<MySmallBusinessLedger.Core.Students.StudentScheduleEntry> schedule,
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
