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

    public StartupDashboardQuery(
        IStudentRepository studentRepository,
        IGoogleCalendarGateway calendarGateway,
        IInvoiceRepository invoiceRepository,
        IClock clock)
    {
        this.studentRepository = studentRepository;
        this.calendarGateway = calendarGateway;
        this.invoiceRepository = invoiceRepository;
        this.clock = clock;
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
            status => status.PaymentStatus);

        var lessons = calendarLessons
            .Where(lesson => currentStudent is null || lesson.StudentShortName == currentStudent)
            .OrderBy(lesson => lesson.StartsAt)
            .Select(lesson =>
            {
                var isInvoiced = statusLookup.TryGetValue((lesson.StudentShortName, lesson.StartsAt), out var paymentStatus);
                var state = isInvoiced ? LessonState.Completed : lesson.State;
                return new DashboardLesson(lesson.Id, lesson.StudentShortName, lesson.StartsAt, state, isInvoiced, isInvoiced ? paymentStatus : null);
            })
            .ToArray();

        var invoices = currentStudent is null
            ? []
            : await invoiceRepository.ListInvoicesForStudentAsync(currentStudent, cancellationToken);

        var studentShortNames = students
            .Select(student => student.Student.ShortName)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return OperationResult<StartupDashboardResult>.Success(
            new StartupDashboardResult(currentStudent, studentShortNames, lessons, invoices));
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
}

internal static class DateOnlyExtensions
{
    public static DateOnly WithDay(this DateOnly date, int day) => new(date.Year, date.Month, day);
}
