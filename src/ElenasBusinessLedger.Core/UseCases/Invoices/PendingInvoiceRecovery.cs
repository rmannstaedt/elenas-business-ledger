using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

public sealed record PendingInvoiceRecovery(
    IssuedInvoice SavedInvoice,
    InvoiceDraft RequestedDraft,
    InvoiceTotals RequestedTotals,
    bool CanRevise);

public enum InvoiceRecoveryChoice
{
    Cancel,
    Resume,
    Revise
}
