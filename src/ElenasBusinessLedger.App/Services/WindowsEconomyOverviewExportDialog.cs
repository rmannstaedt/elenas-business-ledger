using Microsoft.Win32;

namespace ElenasBusinessLedger.App.Services;

public sealed class WindowsEconomyOverviewExportDialog : IEconomyOverviewExportDialog
{
    public string? ShowSaveCsvDialog(int year)
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".csv",
            FileName = $"Economy Overview {year}.csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            OverwritePrompt = true,
            Title = "Export Economy Overview"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
