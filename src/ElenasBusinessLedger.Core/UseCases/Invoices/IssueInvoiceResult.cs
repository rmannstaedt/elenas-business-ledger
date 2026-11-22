using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

// Reports committed issuance separately from printing so a print failure leaves the invoice available for reprint.
public sealed record IssueInvoiceResult(
    IssuedInvoice Invoice,
    bool PrintSucceeded,
    IReadOnlyList<ValidationError> PrintErrors);
