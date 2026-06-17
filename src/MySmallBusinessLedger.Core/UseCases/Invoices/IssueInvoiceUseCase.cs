using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Invoices;

public sealed class IssueInvoiceUseCase
{
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IInvoiceFileStore fileStore;
    private readonly IInvoicePdfWriter pdfWriter;
    private readonly IInvoicePrinter printer;

    public IssueInvoiceUseCase(
        ICompanyProfileRepository companyProfileRepository,
        IInvoiceRepository invoiceRepository,
        IInvoiceFileStore fileStore,
        IInvoicePdfWriter pdfWriter,
        IInvoicePrinter printer)
    {
        this.companyProfileRepository = companyProfileRepository;
        this.invoiceRepository = invoiceRepository;
        this.fileStore = fileStore;
        this.pdfWriter = pdfWriter;
        this.printer = printer;
    }

    public async Task<OperationResult<IssueInvoiceResult>> ExecuteAsync(
        InvoiceDraft draft,
        CancellationToken cancellationToken)
    {
        var company = await companyProfileRepository.GetAsync(cancellationToken);
        var invoiceNumber = await invoiceRepository.GetNextInvoiceNumberAsync(cancellationToken);
        var totals = draft.CalculateTotals(company);

        var stagingPath = await fileStore.CreateStagingPathAsync(draft, invoiceNumber, cancellationToken);
        if (!stagingPath.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(stagingPath.Errors.ToArray());
        }

        var write = await pdfWriter.WriteAsync(draft, totals, invoiceNumber, stagingPath.Value!, cancellationToken);
        if (!write.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(write.Errors.ToArray());
        }

        var verify = await fileStore.VerifyStagedPdfAsync(stagingPath.Value!, cancellationToken);
        if (!verify.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(verify.Errors.ToArray());
        }

        var publish = await fileStore.PublishAsync(stagingPath.Value!, draft, invoiceNumber, cancellationToken);
        if (!publish.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(publish.Errors.ToArray());
        }

        var issuedInvoice = draft.Issue(invoiceNumber, totals, publish.Value!);
        var save = await invoiceRepository.SaveIssuedInvoiceAsync(issuedInvoice, cancellationToken);
        if (!save.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(save.Errors.ToArray());
        }

        var print = await printer.PrintAsync(publish.Value!, cancellationToken);
        return OperationResult<IssueInvoiceResult>.Success(new IssueInvoiceResult(
            issuedInvoice,
            print.Succeeded,
            print.Errors));
    }
}
