using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Time;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

public sealed class ReprintIssuedInvoiceUseCase
{
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IInvoicePrinter printer;
    private readonly IClock clock;

    public ReprintIssuedInvoiceUseCase(
        ICompanyProfileRepository companyProfileRepository,
        IInvoiceRepository invoiceRepository,
        IInvoicePrinter printer,
        IClock clock)
    {
        this.companyProfileRepository = companyProfileRepository;
        this.invoiceRepository = invoiceRepository;
        this.printer = printer;
        this.clock = clock;
    }

    public async Task<OperationResult<ReprintInvoiceResult>> ExecuteAsync(
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetIssuedInvoiceAsync(invoiceNumber, cancellationToken);
        if (invoice is null)
        {
            return OperationResult<ReprintInvoiceResult>.Failure(new ValidationError(
                "Invoice.NotFound",
                $"Invoice {invoiceNumber} could not be found."));
        }

        if (string.IsNullOrWhiteSpace(invoice.PdfFilePath))
        {
            return OperationResult<ReprintInvoiceResult>.Failure(new ValidationError(
                "Invoice.PdfMissing",
                $"Invoice {invoiceNumber} does not have a published PDF to print."));
        }

        var company = await companyProfileRepository.GetAsync(cancellationToken);
        var print = await printer.PrintAsync(
            invoice.PdfFilePath,
            company.ConfiguredInvoicePrinterName,
            cancellationToken);

        var printStatusUpdate = await invoiceRepository.UpdatePrintStatusAsync(
            invoiceNumber,
            print.Succeeded ? InvoicePrintStatus.Printed : InvoicePrintStatus.PrintFailed,
            print.Succeeded ? clock.Now : null,
            print.Succeeded ? null : print.Errors.FirstOrDefault()?.Message,
            cancellationToken);

        if (!printStatusUpdate.Succeeded)
        {
            return OperationResult<ReprintInvoiceResult>.Failure(printStatusUpdate.Errors.ToArray());
        }

        return OperationResult<ReprintInvoiceResult>.Success(new ReprintInvoiceResult(
            printStatusUpdate.Value!,
            print.Succeeded,
            print.Errors));
    }
}
