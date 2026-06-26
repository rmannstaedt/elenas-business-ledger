using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;

namespace MySmallBusinessLedger.Core.UseCases.Invoices;

public sealed record ReprintInvoiceResult(
    IssuedInvoice Invoice,
    bool PrintSucceeded,
    IReadOnlyList<ValidationError> PrintErrors);
