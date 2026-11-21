using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.Ports;

public interface IInvoiceRepository
{
    Task<IReadOnlyList<InvoiceLessonStatus>> ListLessonStatusesAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken, IReadOnlyList<int>? includeLessonIds = null);

    Task<IReadOnlyList<IssuedInvoiceSummary>> ListInvoicesForStudentAsync(int studentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EconomyInvoiceRecord>> ListIssuedInvoiceOverviewRowsAsync(
        int periodYear,
        CancellationToken cancellationToken);

    Task<bool> HasInvoiceForStudentInMonthAsync(int studentId, DateOnly month, CancellationToken cancellationToken);

    Task<InvoiceNumber> GetNextInvoiceNumberAsync(CancellationToken cancellationToken);

    Task<OperationResult<IssuedInvoice>> ReserveInvoiceAsync(
        InvoiceDraft draft,
        InvoiceTotals totals,
        CancellationToken cancellationToken);

    Task<OperationResult<IssuedInvoice>> FinalizeIssuedInvoiceWithLessonLocksAsync(
        InvoiceNumber invoiceNumber,
        string pdfFilePath,
        IReadOnlyList<int> lessonIds,
        CancellationToken cancellationToken);

    Task<OperationResult<IssuedInvoice>> MarkInvoicePdfPublishedAsync(
        InvoiceNumber invoiceNumber,
        string pdfFilePath,
        CancellationToken cancellationToken);

    Task<OperationResult<IssuedInvoice>> ReviseInvoiceReservationAsync(
        IssuedInvoice savedInvoice, InvoiceDraft draft, InvoiceTotals totals, CancellationToken cancellationToken);

    Task<OperationResult<IssuedInvoice>> PrepareInvoicePublicationAsync(
        IssuedInvoice savedInvoice, InvoicePdfPublication publication, CancellationToken cancellationToken);

    Task ReleaseInvoiceReservationAsync(
        InvoiceNumber invoiceNumber, CancellationToken cancellationToken, string? expectedRevision = null);

    Task<OperationResult<IssuedInvoice>> UpdatePrintStatusAsync(
        InvoiceNumber invoiceNumber,
        InvoicePrintStatus printStatus,
        DateTimeOffset? printedAt,
        string? lastPrintError,
        CancellationToken cancellationToken);

    Task<IssuedInvoice?> GetIssuedInvoiceAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);

    Task<IssuedInvoice?> GetIssuingInvoiceAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);

    Task<IssuedInvoice?> GetOldestPendingAsync(CancellationToken cancellationToken);

    Task<OperationResult<InvoiceNumber>> DiscardPendingAsync(
        IssuedInvoice savedInvoice, CancellationToken cancellationToken);

    Task<PaymentStatus> GetPaymentStatusAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);

    Task SavePaymentStatusAsync(InvoiceNumber invoiceNumber, PaymentStatus paymentStatus, CancellationToken cancellationToken);
}
