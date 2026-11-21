using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.UseCases.Economy;

namespace ElenasBusinessLedger.App.Services;

public interface IEconomyOverviewCsvFileService
{
    Task<OperationResult<string>> SaveAsync(
        EconomyOverview overview,
        string filePath,
        CancellationToken cancellationToken);
}
