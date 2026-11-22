using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.UseCases.Economy;

namespace ElenasBusinessLedger.App.Services;

// Supplies the Economy read model and saves eligible legacy amounts for the overview window.
public interface IEconomyOverviewService
{
    Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken);

    Task<OperationResult<EconomyOverview>> SaveManualMonthOverridesAsync(
        int overviewYear,
        EconomySettings settings,
        IReadOnlyList<EconomyManualMonthOverride> overrides,
        CancellationToken cancellationToken);
}
