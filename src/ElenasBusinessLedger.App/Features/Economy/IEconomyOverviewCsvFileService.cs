using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.UseCases.Economy;

namespace ElenasBusinessLedger.App.Services;

// Lets the Economy view model save an export through a replaceable file writer.
public interface IEconomyOverviewCsvFileService
{
    Task<OperationResult<string>> SaveAsync(
        EconomyOverview overview,
        string filePath,
        CancellationToken cancellationToken);
}
