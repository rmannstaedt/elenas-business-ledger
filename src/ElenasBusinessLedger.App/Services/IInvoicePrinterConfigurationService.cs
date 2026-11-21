using ElenasBusinessLedger.Core.Common;

namespace ElenasBusinessLedger.App.Services;

// Resolves or selects a printer before the issue and reprint dialogs send a request.
public interface IInvoicePrinterConfigurationService
{
    Task<OperationResult<bool>> EnsureConfiguredAsync(CancellationToken cancellationToken);
}
