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
    private readonly ILessonRepository lessonRepository;
    private readonly IPlanCatalogRepository planCatalogRepository;

    public InvoicePreviewUseCase(
        IStudentRepository studentRepository,
        ICompanyProfileRepository companyProfileRepository,
        ILessonRepository lessonRepository,
        IPlanCatalogRepository planCatalogRepository)
    {
        this.studentRepository = studentRepository;
        this.companyProfileRepository = companyProfileRepository;
        this.lessonRepository = lessonRepository;
        this.planCatalogRepository = planCatalogRepository;
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
        return await CreateFromLocalLessonsAsync(student, company, invoiceDate, from, to, cancellationToken);
    }

    private async Task<OperationResult<InvoicePreviewResult>> CreateFromLocalLessonsAsync(
        StudentRecord student,
        MySmallBusinessLedger.Core.Company.CompanyProfile company,
        DateOnly invoiceDate,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var month = new DateOnly(from.Year, from.Month, 1);
        var lessons = await lessonRepository.ListForStudentMonthAsync(student.Student.ShortName, month, cancellationToken);
        var matchingLessons = lessons
            .Where(lesson =>
            {
                var date = DateOnly.FromDateTime(lesson.StartsAt.DateTime);
                return date >= from && date <= to;
            })
            .Where(lesson => lesson.InvoiceNumber is null)
            .OrderBy(lesson => lesson.StartsAt)
            .ToArray();
        var errors = new List<ValidationError>();
        var lines = new List<InvoiceLineDraft>();

        foreach (var lesson in matchingLessons)
        {
            var plan = await planCatalogRepository.GetAsync(lesson.PlanCatalogId, cancellationToken);
            if (plan?.PriceWithoutVat is null)
            {
                errors.Add(new ValidationError(
                    "Invoice.NoPlanForLesson",
                    $"No invoice-ready plan was found for {student.Student.ShortName} on {lesson.StartsAt:yyyy-MM-dd}."));
                continue;
            }

            lines.Add(new InvoiceLineDraft(
                lesson.StartsAt,
                string.IsNullOrWhiteSpace(lesson.PlanName) ? plan.Name : lesson.PlanName,
                MoneyAmount.Dkk(plan.PriceWithoutVat.Value),
                lesson.PlanCatalogId,
                lesson.Id,
                lesson.HasKnownStartTime));
        }

        if (errors.Count > 0)
        {
            return OperationResult<InvoicePreviewResult>.Failure(errors.ToArray());
        }

        var draftResult = InvoiceDraft.TryCreate(
            invoiceDate,
            new CompanySnapshot(
                company.Name,
                company.Address,
                company.Cvr,
                company.PaymentInstructions,
                company.BankRegistrationNumber,
                company.BankAccountNumber,
                company.MobilePayAccount),
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
