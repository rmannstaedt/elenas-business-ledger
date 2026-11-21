using ElenasBusinessLedger.Core.Common;

namespace ElenasBusinessLedger.App.Services;

public interface IInvoicePrinterConfigurationService
{
    Task<OperationResult<bool>> EnsureConfiguredAsync(CancellationToken cancellationToken);
}
