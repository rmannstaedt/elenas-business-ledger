namespace ElenasBusinessLedger.App.Services;

// Lets the user choose an Economy CSV path before the view model writes an export.
public interface IEconomyOverviewExportDialog
{
    string? ShowSaveCsvDialog(int year);
}
