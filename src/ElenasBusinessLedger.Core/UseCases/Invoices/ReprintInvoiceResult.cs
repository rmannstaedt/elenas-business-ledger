using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

// Returns the updated invoice and the print request outcome so the viewer can refresh its status.
public sealed record ReprintInvoiceResult(
    IssuedInvoice Invoice,
    bool PrintSucceeded,
    IReadOnlyList<ValidationError> PrintErrors);
