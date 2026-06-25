using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.Time;

namespace MySmallBusinessLedger.Core.UseCases.Startup;

public sealed class StartupDashboardQuery
{
    private readonly IStudentRepository studentRepository;
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IClock clock;
    private readonly TimeZoneInfo lessonTimeZone;

    public StartupDashboardQuery(
        IStudentRepository studentRepository,
        IGoogleCalendarGateway calendarGateway,
        IInvoiceRepository invoiceRepository,
        IClock clock,
        TimeZoneInfo? lessonTimeZone = null)
    {
        this.studentRepository = studentRepository;
        this.calendarGateway = calendarGateway;
        this.invoiceRepository = invoiceRepository;
        this.clock = clock;
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

        var lessons = calendarLessons
            .Where(lesson => currentStudent is null || lesson.StudentShortName == currentStudent)
            .OrderBy(lesson => lesson.StartsAt)
            .Select(lesson =>
            {
                if (!statusLookup.TryGetValue((lesson.StudentShortName, lesson.StartsAt), out var invoiceStatus))
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
            })
            .ToArray();

        var invoices = currentStudent is null
            ? []
            : await invoiceRepository.ListInvoicesForStudentAsync(currentStudent, cancellationToken);

        var studentShortNames = students
            .Select(student => student.Student.ShortName)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var studentNavigationEntries = CreateStudentNavigationEntries(selectedMonth, calendarLessons, students);

        return OperationResult<StartupDashboardResult>.Success(
            new StartupDashboardResult(
                currentStudent,
                studentShortNames,
                lessons,
                invoices,
                CurrentStudentHasSchedule(students, currentStudent),
                studentNavigationEntries));
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
