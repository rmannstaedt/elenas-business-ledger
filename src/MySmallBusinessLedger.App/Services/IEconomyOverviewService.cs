using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.UseCases.Economy;

namespace MySmallBusinessLedger.App.Services;

public interface IEconomyOverviewService
{
    Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken);
}
