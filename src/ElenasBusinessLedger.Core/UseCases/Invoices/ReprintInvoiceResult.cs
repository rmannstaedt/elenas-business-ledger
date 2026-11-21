using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

public sealed record ReprintInvoiceResult(
    IssuedInvoice Invoice,
    bool PrintSucceeded,
    IReadOnlyList<ValidationError> PrintErrors);
