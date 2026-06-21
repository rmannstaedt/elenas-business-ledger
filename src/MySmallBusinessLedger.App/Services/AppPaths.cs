using System.IO;

namespace MySmallBusinessLedger.App.Services;

public sealed record AppPaths(
    string AppDataFolderPath,
    string DatabasePath,
    string GoogleTokenFolderPath,
    string WindowPlacementPath)
{
    private const string CompanyFolderName = "visualtech.dk";
    private const string ApplicationFolderName = "MySmallBusinessLedger";
    private const string DatabaseFileName = "ledger.db";
    private const string GoogleTokenFolderName = "GoogleTokens";
    private const string WindowPlacementFileName = "window-placement.json";

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
