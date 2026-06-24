using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;
using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Core.UseCases.Invoices;

public sealed class InvoicePreviewUseCase
{
    private readonly IStudentRepository studentRepository;
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly IGoogleCalendarGateway calendarGateway;

    public InvoicePreviewUseCase(
        IStudentRepository studentRepository,
        ICompanyProfileRepository companyProfileRepository,
        IGoogleCalendarGateway calendarGateway)
    {
        this.studentRepository = studentRepository;
        this.companyProfileRepository = companyProfileRepository;
        this.calendarGateway = calendarGateway;
    }

    public async Task<OperationResult<InvoicePreviewResult>> CreateAsync(
        string studentShortName,
        DateOnly invoiceDate,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var student = await studentRepository.GetByShortNameAsync(studentShortName, cancellationToken);
        if (student is null)
        {
            return OperationResult<InvoicePreviewResult>.Failure(new ValidationError(
                "Student.NotFound",
                "The selected student could not be found."));
        }

        var company = await companyProfileRepository.GetAsync(cancellationToken);
        var lessons = await calendarGateway.ListLessonsAsync(from, to, cancellationToken);
        var matchingLessons = lessons
            .Where(lesson => lesson.StudentShortName == studentShortName)
            .OrderBy(lesson => lesson.StartsAt)
            .ToArray();
        var errors = new List<ValidationError>();
        var lines = new List<InvoiceLineDraft>();

        foreach (var lesson in matchingLessons)
        {
            var plan = StudentPlanPeriod.GetActivePlan(student.PlanPeriods, DateOnly.FromDateTime(lesson.StartsAt.DateTime));
            if (plan is null)
            {
                errors.Add(new ValidationError(
                    "Invoice.NoActivePlanForLesson",
                    $"No active plan was found for {studentShortName} on {lesson.StartsAt:yyyy-MM-dd}."));
                continue;
            }

            lines.Add(new InvoiceLineDraft(
                lesson.StartsAt,
                plan.Name,
                MoneyAmount.Dkk(plan.PriceWithoutVat),
                plan.PlanCatalogId));
        }

        if (errors.Count > 0)
        {
            return OperationResult<InvoicePreviewResult>.Failure(errors.ToArray());
        }

        var draftResult = InvoiceDraft.TryCreate(
            invoiceDate,
            new CompanySnapshot(company.Name, company.Address, company.Cvr, company.PaymentInstructions),
            new StudentSnapshot(student.Student.ShortName, student.Student.FullName, student.Student.Address),
            lines.ToArray(),
            priceAdjustment: null,
            period: new InvoicePeriod(from, to));

        if (!draftResult.Succeeded)
        {
            return OperationResult<InvoicePreviewResult>.Failure(draftResult.Errors.ToArray());
        }

        var draft = draftResult.Value!;
        return OperationResult<InvoicePreviewResult>.Success(new InvoicePreviewResult(draft, draft.CalculateTotals(company)));
    }
}
