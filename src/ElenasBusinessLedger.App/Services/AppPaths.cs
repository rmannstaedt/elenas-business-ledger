using System.IO;

namespace ElenasBusinessLedger.App.Services;

// Resolves the ledger, authorization store, and layouts from the current launch's data folder.
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
    private const string UserTestEnvironmentVariable = "ELENAS_LEDGER_USER_TEST";

    internal static bool IsUserTestLaunch =>
        Environment.GetEnvironmentVariable(UserTestEnvironmentVariable) is not null;

    // Keeps the debugger's test data together while normal launches find their existing ledger.
    public static AppPaths CreateDefault() => CreateDefault(IsUserTestLaunch);

    internal static AppPaths CreateDefault(bool useUserTestProfile)
    {
        if (useUserTestProfile)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Path.IsPathFullyQualified(localAppData))
            {
                throw new InvalidOperationException("The Windows local app data folder is unavailable for the test ledger.");
            }

            return FromAppDataFolder(Path.Combine(localAppData, "ElenasBusinessLedger", "UserTest"));
        }

        var appDataFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            CompanyFolderName,
            ApplicationFolderName);
        return FromAppDataFolder(appDataFolderPath);
    }

    // Builds all data locations from one root so the selected ledger and its Google credentials stay together.
    public static AppPaths FromAppDataFolder(string appDataFolderPath) =>
        new(
            appDataFolderPath,
            Path.Combine(appDataFolderPath, DatabaseFileName),
            Path.Combine(appDataFolderPath, GoogleTokenFolderName),
            Path.Combine(appDataFolderPath, WindowPlacementFileName));
}
