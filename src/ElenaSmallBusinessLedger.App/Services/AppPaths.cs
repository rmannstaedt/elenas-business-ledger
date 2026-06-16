using System.IO;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed record AppPaths(
    string AppDataFolderPath,
    string DatabasePath,
    string GoogleClientSecretsPath,
    string GoogleTokenFolderPath)
{
    private const string ApplicationFolderName = "ElenaSmallBusinessLedger";
    private const string DatabaseFileName = "ledger.db";
    private const string GoogleClientSecretsFileName = "google-client-secrets.json";
    private const string GoogleTokenFolderName = "GoogleTokens";

    public static AppPaths CreateDefault()
    {
        var appDataFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationFolderName);
        return FromAppDataFolder(appDataFolderPath);
    }

    public static AppPaths FromAppDataFolder(string appDataFolderPath) =>
        new(
            appDataFolderPath,
            Path.Combine(appDataFolderPath, DatabaseFileName),
            Path.Combine(appDataFolderPath, GoogleClientSecretsFileName),
            Path.Combine(appDataFolderPath, GoogleTokenFolderName));
}
