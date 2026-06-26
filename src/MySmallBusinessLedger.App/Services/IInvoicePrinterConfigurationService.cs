using MySmallBusinessLedger.Core.Common;

namespace MySmallBusinessLedger.App.Services;

public interface IInvoicePrinterConfigurationService
{
    Task<OperationResult<bool>> EnsureConfiguredAsync(CancellationToken cancellationToken);
}
