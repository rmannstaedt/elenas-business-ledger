using MySmallBusinessLedger.App.Services;

namespace MySmallBusinessLedger.Tests.App.Services;

public sealed class AppPathsTests
{
    [Fact]
    public void CreateDefaultUsesVisualtechRoamingAppDataFolder()
    {
        var expectedRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "visualtech.dk",
            "MySmallBusinessLedger");

        var paths = AppPaths.CreateDefault();

        Assert.Equal(expectedRoot, paths.AppDataFolderPath);
        Assert.Equal(Path.Combine(expectedRoot, "ledger.db"), paths.DatabasePath);
        Assert.Equal(Path.Combine(expectedRoot, "google-client-secrets.json"), paths.GoogleClientSecretsPath);
        Assert.Equal(Path.Combine(expectedRoot, "GoogleTokens"), paths.GoogleTokenFolderPath);
        Assert.Equal(Path.Combine(expectedRoot, "window-placement.json"), paths.WindowPlacementPath);
    }
}
