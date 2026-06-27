using System.IO;
using System.Text;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.UseCases.Economy;

namespace MySmallBusinessLedger.App.Services;

public sealed class EconomyOverviewCsvFileService : IEconomyOverviewCsvFileService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);
    private readonly EconomyOverviewCsvFormatter formatter;

    public EconomyOverviewCsvFileService(EconomyOverviewCsvFormatter formatter)
    {
        this.formatter = formatter;
    }

    public async Task<OperationResult<string>> SaveAsync(
        EconomyOverview overview,
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            await File.WriteAllTextAsync(filePath, formatter.Format(overview), Utf8WithoutBom, cancellationToken);
            return OperationResult<string>.Success(filePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<string>.Failure(
                new ValidationError("EconomyOverview.ExportFailed", ex.Message));
        }
    }
}
