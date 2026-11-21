namespace ElenasBusinessLedger.App.Services;

public interface IEconomyOverviewExportDialog
{
    string? ShowSaveCsvDialog(int year);
}
