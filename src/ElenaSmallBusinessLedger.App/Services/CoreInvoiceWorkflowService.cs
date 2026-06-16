using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Ports;
using ElenaSmallBusinessLedger.Core.UseCases.Invoices;
using ElenaSmallBusinessLedger.Infrastructure.Invoices.Pdf;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class CoreInvoiceWorkflowService : IInvoiceWorkflowService
{
    private readonly InvoiceCandidateSelectionUseCase candidateSelectionUseCase;
    private readonly InvoicePreviewUseCase previewUseCase;
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IInvoicePdfWriter pdfWriter;
    private readonly IInvoicePrinter printer;

    public CoreInvoiceWorkflowService(
        InvoiceCandidateSelectionUseCase candidateSelectionUseCase,
        InvoicePreviewUseCase previewUseCase,
        ICompanyProfileRepository companyProfileRepository,
        IInvoiceRepository invoiceRepository,
        IInvoicePdfWriter pdfWriter,
        IInvoicePrinter printer)
    {
        this.candidateSelectionUseCase = candidateSelectionUseCase;
        this.previewUseCase = previewUseCase;
        this.companyProfileRepository = companyProfileRepository;
        this.invoiceRepository = invoiceRepository;
        this.pdfWriter = pdfWriter;
        this.printer = printer;
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
        CancellationToken cancellationToken)
    {
        try
        {
            return await previewUseCase.CreateAsync(studentShortName, invoiceDate, from, to, cancellationToken);
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
        CancellationToken cancellationToken)
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
                printer);

            return await issueUseCase.ExecuteAsync(draft, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<IssueInvoiceResult>.Failure(
                new ValidationError("Invoice.IssueFailed", ex.Message));
        }
    }

    public Task<IssuedInvoice?> GetIssuedInvoiceAsync(
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken) =>
        invoiceRepository.GetIssuedInvoiceAsync(invoiceNumber, cancellationToken);
}
