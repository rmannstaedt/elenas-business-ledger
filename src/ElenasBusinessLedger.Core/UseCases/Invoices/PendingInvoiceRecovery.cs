using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

// Gives the recovery dialog the saved snapshot and current proposal so the user can choose how to proceed.
public sealed record PendingInvoiceRecovery(
    IssuedInvoice SavedInvoice,
    InvoiceDraft? RequestedDraft,
    InvoiceTotals RequestedTotals,
    bool CanRevise);

public enum InvoiceRecoveryChoice
{
    Cancel,
    Resume,
    Revise,
    Discard
}
