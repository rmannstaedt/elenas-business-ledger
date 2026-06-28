using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.UseCases.Economy;

namespace MySmallBusinessLedger.App.Services;

public interface IEconomyOverviewService
{
    Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken);

    Task<OperationResult<EconomyOverview>> SaveManualMonthOverridesAsync(
        int overviewYear,
        EconomySettings settings,
        IReadOnlyList<EconomyManualMonthOverride> overrides,
        CancellationToken cancellationToken);
}
