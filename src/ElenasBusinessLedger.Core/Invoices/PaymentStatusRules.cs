using ElenasBusinessLedger.Core.Common;

namespace ElenasBusinessLedger.Core.Invoices;

// Defines when a payment status can change so reopening a paid invoice requires confirmation.
public static class PaymentStatusRules
{
    // Marks an outstanding invoice paid, and asks for confirmation before reopening a paid invoice.
    public static OperationResult<PaymentStatus> Toggle(PaymentStatus current, bool confirmMarkOutstanding)
    {
        return current switch
        {
            PaymentStatus.Outstanding => OperationResult<PaymentStatus>.Success(PaymentStatus.Paid),
            PaymentStatus.Paid when confirmMarkOutstanding => OperationResult<PaymentStatus>.Success(PaymentStatus.Outstanding),
            PaymentStatus.Paid => OperationResult<PaymentStatus>.ConfirmationRequired(
                PaymentStatus.Paid,
                new ValidationError("Payment.ConfirmMarkOutstanding", "Marking a paid invoice outstanding requires confirmation.")),
            _ => throw new ArgumentOutOfRangeException(nameof(current), current, "Unknown payment status.")
        };
    }
}
