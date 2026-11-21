using System.Reflection;

namespace ElenasBusinessLedger.App.Services;

// Reads the product version from the app assembly so About shows the build actually running.
public static class ApplicationInfo
{
    public const string ProductName = "Elena's Business Ledger";
    public const string AboutMenuText = "About " + ProductName;

    public static string Version =>
        typeof(ApplicationInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? typeof(ApplicationInfo).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    public static string AboutText => $"{ProductName}{Environment.NewLine}Version {Version}";
}
