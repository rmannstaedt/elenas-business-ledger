using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Invoices;

public sealed class InvoiceCandidateSelectionUseCase
{
    private readonly IStudentRepository studentRepository;
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly IInvoiceRepository invoiceRepository;

    public InvoiceCandidateSelectionUseCase(
        IStudentRepository studentRepository,
        IGoogleCalendarGateway calendarGateway,
        IInvoiceRepository invoiceRepository)
    {
        this.studentRepository = studentRepository;
        this.calendarGateway = calendarGateway;
        this.invoiceRepository = invoiceRepository;
    }

    public async Task<OperationResult<InvoiceCandidateSelectionResult>> ExecuteAsync(
        DateOnly date,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var students = await studentRepository.ListActiveStudentsAsync(cancellationToken);
        var lessons = await calendarGateway.ListLessonsAsync(date, date, cancellationToken);
        var month = new DateOnly(date.Year, date.Month, 1);
        var candidates = new List<string>();

        foreach (var shortName in lessons.Select(lesson => lesson.StudentShortName).Distinct(StringComparer.Ordinal))
        {
            if (!await invoiceRepository.HasInvoiceForStudentInMonthAsync(shortName, month, cancellationToken))
            {
                candidates.Add(shortName);
            }
        }

        var preselected = lessons
            .Where(lesson => lesson.StartsAt == now)
            .Select(lesson => lesson.StudentShortName)
            .FirstOrDefault(shortName => candidates.Contains(shortName, StringComparer.Ordinal));

        var allStudents = students
            .Select(student => student.Student.ShortName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return OperationResult<InvoiceCandidateSelectionResult>.Success(new InvoiceCandidateSelectionResult(
            candidates.Order(StringComparer.Ordinal).ToArray(),
            allStudents,
            preselected));
    }
}
