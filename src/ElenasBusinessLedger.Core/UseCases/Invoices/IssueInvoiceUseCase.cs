using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Time;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

public sealed class IssueInvoiceUseCase
{
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IInvoiceFileStore fileStore;
    private readonly IInvoicePdfWriter pdfWriter;
    private readonly IInvoicePrinter printer;
    private readonly IClock clock;

    public IssueInvoiceUseCase(
        ICompanyProfileRepository companyProfileRepository,
        IInvoiceRepository invoiceRepository,
        IInvoiceFileStore fileStore,
        IInvoicePdfWriter pdfWriter,
        IInvoicePrinter printer,
        IClock clock)
    {
        this.companyProfileRepository = companyProfileRepository;
        this.invoiceRepository = invoiceRepository;
        this.fileStore = fileStore;
        this.pdfWriter = pdfWriter;
        this.printer = printer;
        this.clock = clock;
    }

    public async Task<OperationResult<IssueInvoiceResult>> ExecuteAsync(
        InvoiceDraft draft,
        CancellationToken cancellationToken)
    {
        var lessonIds = draft.Lines
            .Select(line => line.LessonId)
            .ToArray();
        if (lessonIds.Length != draft.Lines.Count ||
            lessonIds.Any(lessonId => lessonId <= 0) ||
            lessonIds.Distinct().Count() != lessonIds.Length)
        {
            return OperationResult<IssueInvoiceResult>.Failure(
                new ValidationError(
                    "Invoice.LessonIdentityMissing",
                    "Every invoice line must reference one unique saved lesson."));
        }

        var company = await companyProfileRepository.GetAsync(cancellationToken);
        var totals = draft.CalculateTotals(company);
        var reserve = await invoiceRepository.ReserveInvoiceAsync(draft, totals, cancellationToken);
        if (!reserve.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(reserve.Errors.ToArray());
        }

        var invoiceNumber = reserve.Value!.InvoiceNumber;
        var publishedPath = reserve.Value.PdfFilePath;

        if (string.IsNullOrWhiteSpace(publishedPath))
        {
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
                return await DiscardStagingMaybeReleaseReservationAndFailAsync(
                    invoiceNumber,
                    stagingPath.Value!,
                    publish.Errors.ToArray(),
                    releaseReservation: ShouldReleaseReservationAfterPublishFailure(publish.Errors),
                    cancellationToken);
            }

            publishedPath = publish.Value!;
            var markPublished = await invoiceRepository.MarkInvoicePdfPublishedAsync(
                invoiceNumber,
                publishedPath,
                cancellationToken);
            if (!markPublished.Succeeded)
            {
                return OperationResult<IssueInvoiceResult>.Failure(markPublished.Errors.ToArray());
            }
        }

        var finalize = await invoiceRepository.FinalizeIssuedInvoiceWithLessonLocksAsync(
            invoiceNumber,
            publishedPath,
            lessonIds,
            cancellationToken);
        if (!finalize.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(finalize.Errors.ToArray());
        }

        var print = await printer.PrintAsync(
            publishedPath,
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
        return await DiscardStagingMaybeReleaseReservationAndFailAsync(
            invoiceNumber,
            stagingPath,
            errors,
            releaseReservation: true,
            cancellationToken);
    }

    private async Task<OperationResult<IssueInvoiceResult>> DiscardStagingMaybeReleaseReservationAndFailAsync(
        InvoiceNumber invoiceNumber,
        string stagingPath,
        IReadOnlyList<ValidationError> errors,
        bool releaseReservation,
        CancellationToken cancellationToken)
    {
        var discard = await fileStore.DiscardStagingPathAsync(stagingPath, cancellationToken);
        if (releaseReservation)
        {
            await invoiceRepository.ReleaseInvoiceReservationAsync(invoiceNumber, cancellationToken);
        }

        if (discard.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(errors.ToArray());
        }

        return OperationResult<IssueInvoiceResult>.Failure(errors.Concat(discard.Errors).ToArray());
    }

    private static bool ShouldReleaseReservationAfterPublishFailure(IEnumerable<ValidationError> errors) =>
        !errors.Any(error => string.Equals(error.Code, "InvoicePdf.FinalFileAlreadyExists", StringComparison.Ordinal));
}
