using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.UseCases.Invoices;

namespace ElenasBusinessLedger.App.Services;

// Supplies the available students and suggested selection to the invoice picker.
public interface IInvoiceCandidateReader
{
    Task<OperationResult<InvoiceCandidateSelectionResult>> LoadCandidatesAsync(
        DateOnly date,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
