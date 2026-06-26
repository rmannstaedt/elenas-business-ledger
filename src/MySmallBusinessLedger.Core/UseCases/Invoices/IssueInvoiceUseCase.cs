using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.Time;

namespace MySmallBusinessLedger.Core.UseCases.Invoices;

public sealed class IssueInvoiceUseCase
{
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IInvoiceFileStore fileStore;
    private readonly IInvoicePdfWriter pdfWriter;
    private readonly IInvoicePrinter printer;
    private readonly IClock clock;
    private readonly ILessonRepository? lessonRepository;

    public IssueInvoiceUseCase(
        ICompanyProfileRepository companyProfileRepository,
        IInvoiceRepository invoiceRepository,
        IInvoiceFileStore fileStore,
        IInvoicePdfWriter pdfWriter,
        IInvoicePrinter printer,
        IClock clock,
        ILessonRepository? lessonRepository = null)
    {
        this.companyProfileRepository = companyProfileRepository;
        this.invoiceRepository = invoiceRepository;
        this.fileStore = fileStore;
        this.pdfWriter = pdfWriter;
        this.printer = printer;
        this.clock = clock;
        this.lessonRepository = lessonRepository;
    }

    public async Task<OperationResult<IssueInvoiceResult>> ExecuteAsync(
        InvoiceDraft draft,
        CancellationToken cancellationToken)
    {
        var company = await companyProfileRepository.GetAsync(cancellationToken);
        var totals = draft.CalculateTotals(company);
        var reserve = await invoiceRepository.ReserveInvoiceAsync(draft, totals, cancellationToken);
        if (!reserve.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(reserve.Errors.ToArray());
        }

        var invoiceNumber = reserve.Value!.InvoiceNumber;

        var stagingPath = await fileStore.CreateStagingPathAsync(draft, invoiceNumber, cancellationToken);
        if (!stagingPath.Succeeded)
        {
            return await ReleaseReservationAndFailAsync(invoiceNumber, stagingPath.Errors.ToArray(), cancellationToken);
        }

        var write = await pdfWriter.WriteAsync(draft, totals, invoiceNumber, stagingPath.Value!, cancellationToken);
        if (!write.Succeeded)
        {
            return await DiscardStagingReleaseReservationAndFailAsync(
                invoiceNumber,
                stagingPath.Value!,
                write.Errors.ToArray(),
                cancellationToken);
        }

        var verify = await fileStore.VerifyStagedPdfAsync(stagingPath.Value!, cancellationToken);
        if (!verify.Succeeded)
        {
            return await DiscardStagingReleaseReservationAndFailAsync(
                invoiceNumber,
                stagingPath.Value!,
                verify.Errors.ToArray(),
                cancellationToken);
        }

        var publish = await fileStore.PublishAsync(stagingPath.Value!, draft, invoiceNumber, cancellationToken);
        if (!publish.Succeeded)
        {
            return await DiscardStagingReleaseReservationAndFailAsync(
                invoiceNumber,
                stagingPath.Value!,
                publish.Errors.ToArray(),
                cancellationToken);
        }

        var finalize = await invoiceRepository.FinalizeIssuedInvoiceAsync(
            invoiceNumber,
            publish.Value!,
            cancellationToken);
        if (!finalize.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(finalize.Errors.ToArray());
        }

        if (lessonRepository is not null)
        {
            var lessonIds = draft.Lines
                .Select(line => line.LessonId)
                .Where(lessonId => lessonId > 0)
                .Distinct()
                .ToArray();
            await lessonRepository.MarkInvoicedAsync(lessonIds, invoiceNumber, cancellationToken);
        }

        var print = await printer.PrintAsync(
            publish.Value!,
            company.ConfiguredInvoicePrinterName,
            cancellationToken);
        var printStatus = print.Succeeded ? InvoicePrintStatus.Printed : InvoicePrintStatus.PrintFailed;
        var printStatusUpdate = await invoiceRepository.UpdatePrintStatusAsync(
            invoiceNumber,
            printStatus,
            print.Succeeded ? clock.Now : null,
            print.Succeeded ? null : print.Errors.FirstOrDefault()?.Message,
            cancellationToken);

        if (!printStatusUpdate.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(printStatusUpdate.Errors.ToArray());
        }

        return OperationResult<IssueInvoiceResult>.Success(new IssueInvoiceResult(
            printStatusUpdate.Value!,
            print.Succeeded,
            print.Errors));
    }

    private async Task<OperationResult<IssueInvoiceResult>> ReleaseReservationAndFailAsync(
        InvoiceNumber invoiceNumber,
        IReadOnlyList<ValidationError> errors,
        CancellationToken cancellationToken)
    {
        await invoiceRepository.ReleaseInvoiceReservationAsync(invoiceNumber, cancellationToken);
        return OperationResult<IssueInvoiceResult>.Failure(errors.ToArray());
    }

    private async Task<OperationResult<IssueInvoiceResult>> DiscardStagingReleaseReservationAndFailAsync(
        InvoiceNumber invoiceNumber,
        string stagingPath,
        IReadOnlyList<ValidationError> errors,
        CancellationToken cancellationToken)
    {
        var discard = await fileStore.DiscardStagingPathAsync(stagingPath, cancellationToken);
        await invoiceRepository.ReleaseInvoiceReservationAsync(invoiceNumber, cancellationToken);

        if (discard.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(errors.ToArray());
        }

        return OperationResult<IssueInvoiceResult>.Failure(errors.Concat(discard.Errors).ToArray());
    }
}
