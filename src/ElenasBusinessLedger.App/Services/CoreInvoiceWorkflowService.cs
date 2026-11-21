using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Time;
using ElenasBusinessLedger.Core.UseCases.Invoices;
using ElenasBusinessLedger.Infrastructure.Invoices.Pdf;

namespace ElenasBusinessLedger.App.Services;

// Adapts WPF requests to the invoice use cases and resolves the configured invoice folder for each issue or resume attempt.
public sealed class CoreInvoiceWorkflowService : IInvoiceWorkflowService
{
    private readonly InvoiceCandidateSelectionUseCase candidateSelectionUseCase;
    private readonly InvoicePreviewUseCase previewUseCase;
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IInvoicePdfWriter pdfWriter;
    private readonly IInvoicePrinter printer;
    private readonly IClock clock;

    public CoreInvoiceWorkflowService(
        InvoiceCandidateSelectionUseCase candidateSelectionUseCase,
        InvoicePreviewUseCase previewUseCase,
        ICompanyProfileRepository companyProfileRepository,
        IInvoiceRepository invoiceRepository,
        IInvoicePdfWriter pdfWriter,
        IInvoicePrinter printer,
        IClock clock)
    {
        this.candidateSelectionUseCase = candidateSelectionUseCase;
        this.previewUseCase = previewUseCase;
        this.companyProfileRepository = companyProfileRepository;
        this.invoiceRepository = invoiceRepository;
        this.pdfWriter = pdfWriter;
        this.printer = printer;
        this.clock = clock;
    }

    public async Task<OperationResult<InvoiceCandidateSelectionResult>> LoadCandidatesAsync(
        DateOnly date,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            return await candidateSelectionUseCase.ExecuteAsync(date, now, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<InvoiceCandidateSelectionResult>.Failure(
                new ValidationError("Invoice.CandidateLoadFailed", ex.Message));
        }
    }

    public async Task<OperationResult<InvoicePreviewResult>> CreatePreviewAsync(
        string studentShortName,
        DateOnly invoiceDate,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken,
        int? studentId = null)
    {
        try
        {
            return await previewUseCase.CreateAsync(studentShortName, invoiceDate, from, to, cancellationToken, studentId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<InvoicePreviewResult>.Failure(
                new ValidationError("Invoice.PreviewFailed", ex.Message));
        }
    }

    public async Task<OperationResult<InvoiceTotals>> CalculateTotalsAsync(
        InvoiceDraft draft,
        CancellationToken cancellationToken)
    {
        try
        {
            var company = await companyProfileRepository.GetAsync(cancellationToken);
            return OperationResult<InvoiceTotals>.Success(draft.CalculateTotals(company));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<InvoiceTotals>.Failure(
                new ValidationError("Invoice.TotalCalculationFailed", ex.Message));
        }
    }

    public async Task<OperationResult<IssueInvoiceResult>> IssueAsync(
        InvoiceDraft draft,
        CancellationToken cancellationToken,
        Func<PendingInvoiceRecovery, CancellationToken, Task<InvoiceRecoveryChoice>>? chooseRecovery = null)
    {
        try
        {
            var company = await companyProfileRepository.GetAsync(cancellationToken);
            var fileStore = new LocalInvoiceFileStore(new InvoiceFileStoreOptions(company.InvoiceFolderPath));
            var issueUseCase = new IssueInvoiceUseCase(
                companyProfileRepository,
                invoiceRepository,
                fileStore,
                pdfWriter,
                printer,
                clock);

            return await issueUseCase.ExecuteAsync(draft, cancellationToken, chooseRecovery);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<IssueInvoiceResult>.Failure(
                new ValidationError("Invoice.IssueFailed", ex.Message));
        }
    }

    public Task<IssuedInvoice?> GetOldestPendingAsync(CancellationToken cancellationToken) =>
        invoiceRepository.GetOldestPendingAsync(cancellationToken);

    public Task<OperationResult<InvoiceNumber>> DiscardPendingAsync(IssuedInvoice invoice, CancellationToken cancellationToken) =>
        invoiceRepository.DiscardPendingAsync(invoice, cancellationToken);

    public async Task<OperationResult<IssueInvoiceResult>> ResumeAsync(
        InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
    {
        try
        {
            var company = await companyProfileRepository.GetAsync(cancellationToken);
            var fileStore = new LocalInvoiceFileStore(new InvoiceFileStoreOptions(company.InvoiceFolderPath));
            var useCase = new IssueInvoiceUseCase(companyProfileRepository, invoiceRepository, fileStore, pdfWriter, printer, clock);
            return await useCase.ResumeAsync(invoiceNumber, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<IssueInvoiceResult>.Failure(new ValidationError("Invoice.ResumeFailed", ex.Message));
        }
    }

    public async Task<OperationResult<ReprintInvoiceResult>> ReprintAsync(
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var reprintUseCase = new ReprintIssuedInvoiceUseCase(
                companyProfileRepository,
                invoiceRepository,
                printer,
                clock);

            return await reprintUseCase.ExecuteAsync(invoiceNumber, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<ReprintInvoiceResult>.Failure(
                new ValidationError("Invoice.ReprintFailed", ex.Message));
        }
    }

    public Task<IssuedInvoice?> GetIssuedInvoiceAsync(
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken) =>
        invoiceRepository.GetIssuedInvoiceAsync(invoiceNumber, cancellationToken);
}
