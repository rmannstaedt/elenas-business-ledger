using ElenasBusinessLedger.App.Services;

namespace ElenasBusinessLedger.Tests.App.Services;

// Guards the installed app's existing path and verifies that a test launch isolates its data.
public sealed class AppPathsTests
{
    [Fact]
    public void CreateDefaultUsesVisualtechRoamingAppDataFolder()
    {
        var expectedRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "visualtech.dk",
            "MySmallBusinessLedger");

        var paths = AppPaths.CreateDefault(useUserTestProfile: false);

        Assert.Equal(expectedRoot, paths.AppDataFolderPath);
        Assert.Equal(Path.Combine(expectedRoot, "ledger.db"), paths.DatabasePath);
        Assert.Equal(Path.Combine(expectedRoot, "GoogleTokens"), paths.GoogleTokenFolderPath);
        Assert.Equal(Path.Combine(expectedRoot, "window-placement.json"), paths.WindowPlacementPath);
    }

    [Fact]
    public void UserTestLaunchKeepsDatabaseAuthorizationAndLayoutInAnIsolatedFolder()
    {
        var expectedRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ElenasBusinessLedger",
            "UserTest");

        var paths = AppPaths.CreateDefault(useUserTestProfile: true);

        Assert.Equal(expectedRoot, paths.AppDataFolderPath);
        Assert.Equal(Path.Combine(expectedRoot, "ledger.db"), paths.DatabasePath);
        Assert.Equal(Path.Combine(expectedRoot, "GoogleTokens"), paths.GoogleTokenFolderPath);
        Assert.Equal(Path.Combine(expectedRoot, "window-placement.json"), paths.WindowPlacementPath);
        Assert.NotEqual(AppPaths.CreateDefault(useUserTestProfile: false).AppDataFolderPath, paths.AppDataFolderPath);
    }
}
