namespace MySmallBusinessLedger.App.Services;

public interface IEconomyOverviewExportDialog
{
    string? ShowSaveCsvDialog(int year);
}
