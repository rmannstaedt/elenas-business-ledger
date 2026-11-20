using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.UseCases.Economy;

namespace MySmallBusinessLedger.App.Services;

public interface IEconomyOverviewCsvFileService
{
    Task<OperationResult<string>> SaveAsync(
        EconomyOverview overview,
        string filePath,
        CancellationToken cancellationToken);
}
