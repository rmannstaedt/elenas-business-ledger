using System.Reflection;

namespace MySmallBusinessLedger.App.Services;

public static class ApplicationInfo
{
    public const string ProductName = "My Small Business Ledger";

    public static string Version =>
        typeof(ApplicationInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? typeof(ApplicationInfo).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    public static string AboutText => $"{ProductName}{Environment.NewLine}Version {Version}";
}
