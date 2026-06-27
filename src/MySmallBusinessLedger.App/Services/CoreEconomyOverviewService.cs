using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.UseCases.Economy;

namespace MySmallBusinessLedger.App.Services;

public sealed class CoreEconomyOverviewService : IEconomyOverviewService
{
    private readonly EconomyOverviewQuery query;

    public CoreEconomyOverviewService(EconomyOverviewQuery query)
    {
        this.query = query;
    }

    public async Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken)
    {
        try
        {
            return OperationResult<EconomyOverview>.Success(await query.LoadAsync(year, cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<EconomyOverview>.Failure(
                new ValidationError("EconomyOverview.LoadFailed", ex.Message));
        }
    }
}
