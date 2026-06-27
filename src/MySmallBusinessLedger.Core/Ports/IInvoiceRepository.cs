using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;

namespace MySmallBusinessLedger.Core.Ports;

public interface IInvoiceRepository
{
    Task<IReadOnlyList<InvoiceLessonStatus>> ListLessonStatusesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<IReadOnlyList<IssuedInvoiceSummary>> ListInvoicesForStudentAsync(string studentShortName, CancellationToken cancellationToken);

    Task<IReadOnlyList<EconomyInvoiceRecord>> ListIssuedInvoiceOverviewRowsAsync(
        int periodYear,
        CancellationToken cancellationToken);

    Task<bool> HasInvoiceForStudentInMonthAsync(string studentShortName, DateOnly month, CancellationToken cancellationToken);

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

    Task ReleaseInvoiceReservationAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);

    Task<OperationResult<IssuedInvoice>> UpdatePrintStatusAsync(
        InvoiceNumber invoiceNumber,
        InvoicePrintStatus printStatus,
        DateTimeOffset? printedAt,
        string? lastPrintError,
        CancellationToken cancellationToken);

    Task<IssuedInvoice?> GetIssuedInvoiceAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);

    Task<IssuedInvoice?> GetIssuingInvoiceAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);

    Task<PaymentStatus> GetPaymentStatusAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);

    Task SavePaymentStatusAsync(InvoiceNumber invoiceNumber, PaymentStatus paymentStatus, CancellationToken cancellationToken);
}
