using System.IO;

namespace ElenasBusinessLedger.App.Services;

// Resolves the current user's database, authorization store, and window placement paths from one data folder.
public sealed record AppPaths(
    string AppDataFolderPath,
    string DatabasePath,
    string GoogleTokenFolderPath,
    string WindowPlacementPath)
{
    private const string CompanyFolderName = "visualtech.dk";
    // Keep locating the same ledger, settings, and tokens after a product rename.
    private const string ApplicationFolderName = "MySmallBusinessLedger";
    private const string DatabaseFileName = "ledger.db";
    private const string GoogleTokenFolderName = "GoogleTokens";
    private const string WindowPlacementFileName = "window-placement.json";

    // Resolves the current user's application data root so the ledger, Google tokens, and layouts follow that Windows account.
    public static AppPaths CreateDefault()
    {
        var appDataFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            CompanyFolderName,
            ApplicationFolderName);
        return FromAppDataFolder(appDataFolderPath);
    }

    public static AppPaths FromAppDataFolder(string appDataFolderPath) =>
        new(
            appDataFolderPath,
            Path.Combine(appDataFolderPath, DatabaseFileName),
            Path.Combine(appDataFolderPath, GoogleTokenFolderName),
            Path.Combine(appDataFolderPath, WindowPlacementFileName));
}
