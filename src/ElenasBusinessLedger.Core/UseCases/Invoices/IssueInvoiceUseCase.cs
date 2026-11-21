using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Time;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

// Coordinates the ledger and PDF store so interrupted issuance can recover the saved invoice number and billing details.
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

    /// <summary>Reserves or recovers a draft and reports issuance separately from the print result.</summary>
    public async Task<OperationResult<IssueInvoiceResult>> ExecuteAsync(
        InvoiceDraft draft,
        CancellationToken cancellationToken,
        Func<PendingInvoiceRecovery, CancellationToken, Task<InvoiceRecoveryChoice>>? chooseRecovery = null)
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

        var savedInvoice = reserve.Value!;
        if (savedInvoice.SnapshotRevision is null || !MatchesSnapshot(savedInvoice, draft, totals))
        {
            var recovery = await ResolvePendingInvoiceAsync(savedInvoice, draft, totals, chooseRecovery, cancellationToken);
            if (!recovery.Succeeded)
            {
                return OperationResult<IssueInvoiceResult>.Failure(recovery.Errors.ToArray());
            }

            savedInvoice = recovery.Value!;
        }

        return await PublishAndPrintAsync(savedInvoice, company.ConfiguredInvoicePrinterName, cancellationToken);
    }

    /// <summary>Finishes the oldest recoverable reservation using its saved snapshot.</summary>
    public async Task<OperationResult<IssueInvoiceResult>> ResumeAsync(
        InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
    {
        var pending = await invoiceRepository.GetOldestPendingAsync(cancellationToken);
        if (pending is null || pending.InvoiceNumber != invoiceNumber || pending.SnapshotRevision is null ||
            (pending.Publication is null && pending.Lines.Count == 0))
        {
            return OperationResult<IssueInvoiceResult>.Failure(new ValidationError(
                "Invoice.PendingNeedsReview", "This reservation changed or requires review of the original invoice details."));
        }

        var company = await companyProfileRepository.GetAsync(cancellationToken);
        return await PublishAndPrintAsync(pending, company.ConfiguredInvoicePrinterName, cancellationToken);
    }

    private async Task<OperationResult<IssueInvoiceResult>> PublishAndPrintAsync(
        IssuedInvoice savedInvoice, string printerName, CancellationToken cancellationToken)
    {
        if (savedInvoice.Publication is null)
        {
            var prepare = await PreparePublicationAsync(savedInvoice, cancellationToken);
            if (!prepare.Succeeded)
            {
                return OperationResult<IssueInvoiceResult>.Failure(prepare.Errors.ToArray());
            }

            savedInvoice = prepare.Value!;
        }

        var publication = await fileStore.CompletePublicationAsync(savedInvoice.Publication!, cancellationToken);
        if (!publication.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(publication.Errors.ToArray());
        }

        var invoiceNumber = savedInvoice.InvoiceNumber;
        var publishedPath = publication.Value!;
        if (savedInvoice.PdfFilePath is null)
        {
            var markPublished = await invoiceRepository.MarkInvoicePdfPublishedAsync(invoiceNumber, publishedPath, cancellationToken);
            if (!markPublished.Succeeded)
            {
                return OperationResult<IssueInvoiceResult>.Failure(markPublished.Errors.ToArray());
            }
        }

        var finalize = await invoiceRepository.FinalizeIssuedInvoiceWithLessonLocksAsync(
            invoiceNumber,
            publishedPath,
            savedInvoice.Lines.Select(line => line.LessonId).ToArray(),
            cancellationToken);
        if (!finalize.Succeeded)
        {
            return OperationResult<IssueInvoiceResult>.Failure(finalize.Errors.ToArray());
        }

        // Issuance is committed here. Print failures must leave the saved invoice available for reprint.
        var issued = finalize.Value!;
        OperationResult<string> print;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            print = await printer.PrintAsync(publishedPath, printerName, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            print = OperationResult<string>.Failure(new ValidationError("InvoicePrint.Cancelled",
                "The invoice was issued. The print request was interrupted."));
        }
        catch (Exception ex)
        {
            print = OperationResult<string>.Failure(new ValidationError("InvoicePrint.Failed", ex.Message));
        }

        var warnings = print.Errors.ToList();
        try
        {
            var update = await invoiceRepository.UpdatePrintStatusAsync(
                invoiceNumber,
                print.Succeeded ? InvoicePrintStatus.Printed : InvoicePrintStatus.PrintFailed,
                print.Succeeded ? clock.Now : null,
                print.Succeeded ? null : print.Errors.FirstOrDefault()?.Message,
                CancellationToken.None);
            if (update.Succeeded)
            {
                return OperationResult<IssueInvoiceResult>.Success(new IssueInvoiceResult(update.Value!, print.Succeeded, warnings));
            }

            warnings.AddRange(update.Errors);
        }
        catch (Exception ex)
        {
            warnings.Add(new ValidationError("InvoicePrint.StatusSaveFailed", $"The invoice was issued. Its print status could not be saved. {ex.Message}"));
        }

        return OperationResult<IssueInvoiceResult>.Success(new IssueInvoiceResult(issued, print.Succeeded, warnings));
    }

    // Ask before choosing between the saved snapshot and a changed draft. Publication freezes the saved details.
    private async Task<OperationResult<IssuedInvoice>> ResolvePendingInvoiceAsync(
        IssuedInvoice savedInvoice,
        InvoiceDraft requestedDraft,
        InvoiceTotals requestedTotals,
        Func<PendingInvoiceRecovery, CancellationToken, Task<InvoiceRecoveryChoice>>? chooseRecovery,
        CancellationToken cancellationToken)
    {
        var legacy = savedInvoice.SnapshotRevision is null;
        if (legacy && savedInvoice.PdfFilePath is not null)
        {
            return OperationResult<IssuedInvoice>.Failure(new ValidationError(
                "Invoice.LegacyPdfNeedsReview",
                $"Pending invoice {savedInvoice.InvoiceNumber} has an older PDF at '{savedInvoice.PdfFilePath}'. Review that PDF and its saved totals before issuing a replacement."));
        }

        var canRevise = savedInvoice.Publication is null && savedInvoice.PdfFilePath is null;
        var choice = chooseRecovery is null
            ? InvoiceRecoveryChoice.Cancel
            : await chooseRecovery(new PendingInvoiceRecovery(savedInvoice, requestedDraft, requestedTotals, canRevise), cancellationToken);

        if (choice == InvoiceRecoveryChoice.Resume && !legacy)
        {
            return OperationResult<IssuedInvoice>.Success(savedInvoice);
        }

        if (choice == InvoiceRecoveryChoice.Revise && canRevise)
        {
            return await invoiceRepository.ReviseInvoiceReservationAsync(
                savedInvoice, requestedDraft, requestedTotals, cancellationToken);
        }

        return OperationResult<IssuedInvoice>.Failure(new ValidationError(
            "Invoice.PendingRecoveryCancelled", $"Pending invoice {savedInvoice.InvoiceNumber} was left as saved."));
    }

    private async Task<OperationResult<IssuedInvoice>> PreparePublicationAsync(
        IssuedInvoice savedInvoice, CancellationToken cancellationToken)
    {
        var draft = savedInvoice.CreateSavedDraft();
        var number = savedInvoice.InvoiceNumber;
        var staging = await fileStore.CreateStagingPathAsync(draft, number, cancellationToken);
        if (!staging.Succeeded)
        {
            await invoiceRepository.ReleaseInvoiceReservationAsync(number, cancellationToken, savedInvoice.SnapshotRevision);
            return OperationResult<IssuedInvoice>.Failure(staging.Errors.ToArray());
        }

        var path = staging.Value!;
        var write = await pdfWriter.WriteAsync(draft, savedInvoice.Totals, number, path, cancellationToken);
        if (!write.Succeeded)
        {
            return await DiscardFailedStagingAsync(savedInvoice, path, write.Errors, cancellationToken);
        }

        var verify = await fileStore.VerifyStagedPdfAsync(path, cancellationToken);
        if (!verify.Succeeded)
        {
            return await DiscardFailedStagingAsync(savedInvoice, path, verify.Errors, cancellationToken);
        }

        var prepare = await fileStore.PreparePublicationAsync(path, draft, number, cancellationToken);
        if (!prepare.Succeeded)
        {
            return await DiscardFailedStagingAsync(savedInvoice, path, prepare.Errors, cancellationToken);
        }

        // Persist the paths and checksum before moving the file so a retry can recognize the same PDF.
        var checkpoint = await invoiceRepository.PrepareInvoicePublicationAsync(
            savedInvoice, prepare.Value!, cancellationToken);
        if (!checkpoint.Succeeded)
        {
            var discard = await fileStore.DiscardStagingPathAsync(path, cancellationToken);
            return OperationResult<IssuedInvoice>.Failure(checkpoint.Errors.Concat(discard.Errors).ToArray());
        }

        return checkpoint;
    }

    private async Task<OperationResult<IssuedInvoice>> DiscardFailedStagingAsync(
        IssuedInvoice savedInvoice, string path, IReadOnlyList<ValidationError> errors, CancellationToken cancellationToken)
    {
        var discard = await fileStore.DiscardStagingPathAsync(path, cancellationToken);
        await invoiceRepository.ReleaseInvoiceReservationAsync(
            savedInvoice.InvoiceNumber, cancellationToken, savedInvoice.SnapshotRevision);
        return OperationResult<IssuedInvoice>.Failure(errors.Concat(discard.Errors).ToArray());
    }

    private static bool MatchesSnapshot(IssuedInvoice saved, InvoiceDraft draft, InvoiceTotals totals)
    {
        if (saved.InvoiceDate != draft.InvoiceDate || saved.Period != draft.Period ||
            saved.Company != draft.Company ||
            saved.Student != (draft.Student with { StudentId = draft.Student.StudentId ?? saved.Student.StudentId }) ||
            saved.PriceAdjustment != draft.PriceAdjustment || saved.Totals != totals ||
            saved.Lines.Count != draft.Lines.Count)
        {
            return false;
        }

        // Compare by lesson ID so a change in display order does not trigger recovery.
        return saved.Lines.OrderBy(line => line.LessonId).Zip(draft.Lines.OrderBy(line => line.LessonId))
            .All(pair => pair.First.LessonId == pair.Second.LessonId &&
                pair.First.LessonDateTime.EqualsExact(pair.Second.LessonDateTime) &&
                pair.First.HasKnownLessonTime == pair.Second.HasKnownLessonTime &&
                pair.First.PlanCatalogId == pair.Second.PlanCatalogId &&
                pair.First.PlanName == pair.Second.PlanName &&
                pair.First.PriceWithoutVat == pair.Second.PriceWithoutVat);
    }
}
