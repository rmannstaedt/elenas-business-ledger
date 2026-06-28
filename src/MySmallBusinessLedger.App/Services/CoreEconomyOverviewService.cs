using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.UseCases.Economy;

namespace MySmallBusinessLedger.App.Services;

public sealed class CoreEconomyOverviewService : IEconomyOverviewService
{
    private readonly EconomyOverviewQuery query;
    private readonly IEconomyRepository? economyRepository;

    public CoreEconomyOverviewService(EconomyOverviewQuery query, IEconomyRepository? economyRepository = null)
    {
        this.query = query;
        this.economyRepository = economyRepository;
    }

    public async Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken)
    {
        try
        {
            return await query.LoadAsync(year, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<EconomyOverview>.Failure(
                new ValidationError("EconomyOverview.LoadFailed", ex.Message));
        }
    }

    public async Task<OperationResult<EconomyOverview>> SaveManualMonthOverridesAsync(
        int overviewYear,
        EconomySettings settings,
        IReadOnlyList<EconomyManualMonthOverride> overrides,
        CancellationToken cancellationToken)
    {
        if (economyRepository is null)
        {
            return OperationResult<EconomyOverview>.Failure(
                new ValidationError("EconomyOverview.ManualOverridesUnavailable", "Manual month totals cannot be saved."));
        }

        try
        {
            await economyRepository.SaveSettingsAndManualMonthOverridesAsync(settings, overrides, cancellationToken);
            return await query.LoadAsync(overviewYear, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<EconomyOverview>.Failure(
                new ValidationError("EconomyOverview.SaveManualOverridesFailed", ex.Message));
        }
    }
}
