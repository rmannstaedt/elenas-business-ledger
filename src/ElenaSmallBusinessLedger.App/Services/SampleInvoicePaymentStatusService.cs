using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Invoices;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class SampleInvoicePaymentStatusService : IInvoicePaymentStatusService
{
    private readonly Dictionary<InvoiceNumber, PaymentStatus> statuses = [];

    public Task<OperationResult<PaymentStatus>> ToggleAsync(
        InvoiceNumber invoiceNumber,
        bool confirmMarkOutstanding,
        CancellationToken cancellationToken)
    {
        var current = statuses.GetValueOrDefault(invoiceNumber, PaymentStatus.Outstanding);
        var next = current == PaymentStatus.Outstanding
            ? PaymentStatus.Paid
            : PaymentStatus.Outstanding;

        statuses[invoiceNumber] = next;
        return Task.FromResult(OperationResult<PaymentStatus>.Success(next));
    }
}
