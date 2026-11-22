using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Time;
using ElenasBusinessLedger.Core.UseCases.Invoices;

namespace ElenasBusinessLedger.App.Services;

// Adapts WPF requests to the invoice use cases and resolves the configured invoice folder for each issue or resume attempt.
public sealed class CoreInvoiceWorkflowService : IInvoiceWorkflowService
{
    private readonly InvoicePreviewUseCase previewUseCase;
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly Func<string, IInvoiceFileStore> createInvoiceFileStore;
    private readonly IInvoicePdfWriter pdfWriter;
    private readonly IInvoicePrinter printer;
    private readonly IClock clock;

    public CoreInvoiceWorkflowService(
        InvoicePreviewUseCase previewUseCase,
        ICompanyProfileRepository companyProfileRepository,
        IInvoiceRepository invoiceRepository,
        Func<string, IInvoiceFileStore> createInvoiceFileStore,
        IInvoicePdfWriter pdfWriter,
        IInvoicePrinter printer,
        IClock clock)
    {
        this.previewUseCase = previewUseCase;
        this.companyProfileRepository = companyProfileRepository;
        this.invoiceRepository = invoiceRepository;
        this.createInvoiceFileStore = createInvoiceFileStore;
        this.pdfWriter = pdfWriter;
        this.printer = printer;
        this.clock = clock;
    }

    // Builds an invoice draft for the requested period so the editor can display amounts before issuance.
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

    // Uses current company VAT settings to calculate the on-screen totals for an edited draft.
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

    // Uses the configured invoice folder to issue the draft or recover a matching pending reservation.
    public async Task<OperationResult<IssueInvoiceResult>> IssueAsync(
        InvoiceDraft draft,
        CancellationToken cancellationToken,
        Func<PendingInvoiceRecovery, CancellationToken, Task<InvoiceRecoveryChoice>>? chooseRecovery = null)
    {
        try
        {
            var issueUseCase = await CreateIssueUseCaseAsync(cancellationToken);
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

    // Reopens the saved reservation using the configured file store and returns the issuance and print outcomes.
    public async Task<OperationResult<IssueInvoiceResult>> ResumeAsync(
        InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
    {
        try
        {
            var issueUseCase = await CreateIssueUseCaseAsync(cancellationToken);
            return await issueUseCase.ResumeAsync(invoiceNumber, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<IssueInvoiceResult>.Failure(new ValidationError("Invoice.ResumeFailed", ex.Message));
        }
    }

    private async Task<IssueInvoiceUseCase> CreateIssueUseCaseAsync(CancellationToken cancellationToken)
    {
        var company = await companyProfileRepository.GetAsync(cancellationToken);
        var fileStore = createInvoiceFileStore(company.InvoiceFolderPath);
        return new IssueInvoiceUseCase(companyProfileRepository, invoiceRepository, fileStore, pdfWriter, printer, clock);
    }

    // Sends the existing invoice to the printer and returns any warnings for the invoice viewer.
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
