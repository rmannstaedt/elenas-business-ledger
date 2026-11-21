using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.UseCases.Economy;

namespace ElenasBusinessLedger.App.Services;

public interface IEconomyOverviewService
{
    Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken);

    Task<OperationResult<EconomyOverview>> SaveManualMonthOverridesAsync(
        int overviewYear,
        EconomySettings settings,
        IReadOnlyList<EconomyManualMonthOverride> overrides,
        CancellationToken cancellationToken);
}
