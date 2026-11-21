using System.IO;
using System.Text;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.UseCases.Economy;

namespace ElenasBusinessLedger.App.Services;

// Writes the formatted Economy report to the user's selected export path.
public sealed class EconomyOverviewCsvFileService : IEconomyOverviewCsvFileService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);
    private readonly EconomyOverviewCsvFormatter formatter;

    public EconomyOverviewCsvFileService(EconomyOverviewCsvFormatter formatter)
    {
        this.formatter = formatter;
    }

    // Writes the displayed Economy report to the chosen UTF-8 CSV file and returns the saved path.
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
