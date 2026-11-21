using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Time;
using ElenasBusinessLedger.Core.UseCases.Invoices;
using ElenasBusinessLedger.Infrastructure.Invoices.Pdf;

namespace ElenasBusinessLedger.App.Services;

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
                printer,
                clock);

            return await issueUseCase.ExecuteAsync(draft, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<IssueInvoiceResult>.Failure(
                new ValidationError("Invoice.IssueFailed", ex.Message));
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
