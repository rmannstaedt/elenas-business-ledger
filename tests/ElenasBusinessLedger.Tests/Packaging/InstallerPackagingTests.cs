using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ElenasBusinessLedger.Tests.Packaging;

public sealed class InstallerPackagingTests
{
    private const string InstallerFileName = "ElenasBusinessLedger-1.4.0-Setup.exe";

    [Fact]
    public void InnoSetupScriptInstallsPerUserAndPreservesAppData()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "installer", "ElenasBusinessLedger.iss"));

        Assert.Contains("#define MyAppVersion \"1.4.0\"", script, StringComparison.Ordinal);
        Assert.Contains("AppId={{8F2B99B4-0C13-4980-8B57-0A58D9CCFC3F}", script, StringComparison.Ordinal);
        Assert.Contains("PrivilegesRequired=lowest", script, StringComparison.Ordinal);
        Assert.Contains(@"DefaultDirName={localappdata}\Programs\MySmallBusinessLedger", script, StringComparison.Ordinal);
        Assert.Contains("#define MyAppName \"Elena's Business Ledger\"", script, StringComparison.Ordinal);
        Assert.Contains("DefaultGroupName={#MyAppName}", script, StringComparison.Ordinal);
        Assert.Contains("UsePreviousGroup=no", script, StringComparison.Ordinal);
        Assert.Contains("#define MyAppExeName \"ElenasBusinessLedger.App.exe\"", script, StringComparison.Ordinal);
        Assert.Contains("#define MyInstalledExeName \"MySmallBusinessLedger.App.exe\"", script, StringComparison.Ordinal);
        Assert.Contains(
            @"Source: ""{#PublishDir}\*""; DestDir: ""{app}""; Excludes: ""\{#MyAppExeName}""; Flags: ignoreversion recursesubdirs createallsubdirs",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            @"Source: ""{#PublishDir}\{#MyAppExeName}""; DestDir: ""{app}""; DestName: ""{#MyInstalledExeName}""; Flags: ignoreversion",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            @"Name: ""{autodesktop}\{#MyAppName}""; Filename: ""{app}\{#MyInstalledExeName}""; Tasks: desktopicon",
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
        Assert.Contains("--framework net11.0-windows", script, StringComparison.Ordinal);
        Assert.Contains("--runtime", script, StringComparison.Ordinal);
        Assert.Contains("win-x64", script, StringComparison.Ordinal);
        Assert.Contains("--self-contained", script, StringComparison.Ordinal);
        Assert.Contains("true", script, StringComparison.Ordinal);
        Assert.Contains(InstallerFileName, script, StringComparison.Ordinal);
        Assert.Contains("artifacts", script, StringComparison.Ordinal);
        Assert.Contains("installer", script, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("win-arm64", false, "ParameterArgumentValidationError")]
    [InlineData("win-x64", false, "exit code 23")]
    [InlineData("win-x64", true, "redirected filesystem entry")]
    public async Task RejectedOrFailedBuildPreservesThePreviousInstaller(string runtime, bool redirectDestination, string expectedError)
    {
        var testRoot = Directory.CreateTempSubdirectory("LedgerInstaller-");
        var token = TestContext.Current.CancellationToken;
        var redirectedPath = Path.Combine(testRoot.FullName, @"artifacts\installer");
        try
        {
            var scriptDir = testRoot.CreateSubdirectory("scripts");
            var projectDir = testRoot.CreateSubdirectory(@"src\ElenasBusinessLedger.App");
            var innoDir = testRoot.CreateSubdirectory("installer");
            testRoot.CreateSubdirectory("artifacts");
            var artifactDir = testRoot.CreateSubdirectory(redirectDestination ? "previous" : @"artifacts\installer");
            var script = Path.Combine(scriptDir.FullName, "build-installer.ps1");
            File.Copy(Path.Combine(FindRepositoryRoot(), "scripts", "build-installer.ps1"), script);
            await File.WriteAllTextAsync(Path.Combine(projectDir.FullName, "ElenasBusinessLedger.App.csproj"), "", token);
            await File.WriteAllTextAsync(Path.Combine(innoDir.FullName, "ElenasBusinessLedger.iss"), "", token);
            var installer = Path.Combine(artifactDir.FullName, InstallerFileName);
            await File.WriteAllTextAsync(installer, "Previous verified installer", token);
            var compiler = Path.Combine(testRoot.FullName, "compiler.ps1");
            await File.WriteAllTextAsync(compiler, "throw 'Compiler should not run'", token);
            var wrapper = Path.Combine(testRoot.FullName, "run.ps1");
            await File.WriteAllTextAsync(wrapper,
                """
                param($Runtime, $RedirectDestination)
                function dotnet { $global:LASTEXITCODE = 23 }
                $ErrorActionPreference = "Stop"
                if ($RedirectDestination -eq "True") {
                    New-Item -ItemType Junction -Path (Join-Path $PSScriptRoot "artifacts\installer") -Target (Join-Path $PSScriptRoot "previous") | Out-Null
                }
                & (Join-Path $PSScriptRoot "scripts\build-installer.ps1") -Runtime $Runtime -InnoSetupCompiler (Join-Path $PSScriptRoot "compiler.ps1")
                """, token);

            var shell = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
            using var process = Process.Start(new ProcessStartInfo(shell)
            {
                ArgumentList = { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", wrapper, runtime, redirectDestination.ToString() },
                UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true
            })!;
            var errors = process.StandardError.ReadToEndAsync(token);
            var output = process.StandardOutput.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);
            var detail = await errors + await output;
            Assert.NotEqual(0, process.ExitCode);
            Assert.Contains(expectedError, detail, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("Previous verified installer", await File.ReadAllTextAsync(installer, token));
        }
        finally
        {
            if (redirectDestination && Directory.Exists(redirectedPath))
            {
                Directory.Delete(redirectedPath);
            }
            testRoot.Delete(recursive: true);
        }
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
                if (File.Exists(Path.Combine(current.FullName, "ElenasBusinessLedger.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        throw new InvalidOperationException("Could not find repository root.");
    }
}
