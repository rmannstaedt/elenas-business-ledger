using System.Runtime.CompilerServices;

namespace MySmallBusinessLedger.Tests.Packaging;

public sealed class InstallerPackagingTests
{
    private const string InstallerFileName = "MySmallBusinessLedger-1.20.0-Setup.exe";

    [Fact]
    public void InnoSetupScriptInstallsPerUserAndPreservesAppData()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "installer", "MySmallBusinessLedger.iss"));

        Assert.Contains("#define MyAppVersion \"1.20.0\"", script, StringComparison.Ordinal);
        Assert.Contains("AppId={{8F2B99B4-0C13-4980-8B57-0A58D9CCFC3F}", script, StringComparison.Ordinal);
        Assert.Contains("PrivilegesRequired=lowest", script, StringComparison.Ordinal);
        Assert.Contains(@"DefaultDirName={localappdata}\Programs\MySmallBusinessLedger", script, StringComparison.Ordinal);
        Assert.Contains(@"DefaultGroupName=My Small Business Ledger", script, StringComparison.Ordinal);
        Assert.Contains(
            @"Source: ""{#PublishDir}\*""; DestDir: ""{app}""; Flags: ignoreversion recursesubdirs createallsubdirs",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            @"Name: ""{autodesktop}\{#MyAppName}""; Filename: ""{app}\{#MyAppExeName}""; Tasks: desktopicon",
            script,
            StringComparison.Ordinal);
        Assert.DoesNotContain("[UninstallDelete]", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{userappdata}", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"\visualtech.dk\MySmallBusinessLedger", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InstallerBuildScriptPublishesSelfContainedWinX64AndRunsInno()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "build-installer.ps1"));

        Assert.Contains(@"C:\Program Files (x86)\Inno Setup 6\ISCC.exe", script, StringComparison.Ordinal);
        Assert.Contains("dotnet", script, StringComparison.Ordinal);
        Assert.Contains("publish", script, StringComparison.Ordinal);
        Assert.Contains("--configuration", script, StringComparison.Ordinal);
        Assert.Contains("Release", script, StringComparison.Ordinal);
        Assert.Contains("--runtime", script, StringComparison.Ordinal);
        Assert.Contains("win-x64", script, StringComparison.Ordinal);
        Assert.Contains("--self-contained", script, StringComparison.Ordinal);
        Assert.Contains("true", script, StringComparison.Ordinal);
        Assert.Contains(InstallerFileName, script, StringComparison.Ordinal);
        Assert.Contains("artifacts", script, StringComparison.Ordinal);
        Assert.Contains("installer", script, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot([CallerFilePath] string sourceFilePath = "")
    {
        foreach (var startPath in new[]
                 {
                     Path.GetDirectoryName(sourceFilePath) ?? string.Empty,
                     Directory.GetCurrentDirectory(),
                     AppContext.BaseDirectory
                 })
        {
            var current = new DirectoryInfo(startPath);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "MySmallBusinessLedger.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        throw new InvalidOperationException("Could not find repository root.");
    }
}
