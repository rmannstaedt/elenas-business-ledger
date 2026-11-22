using System.IO;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace ElenasBusinessLedger.App.Services;

public enum GoogleOAuthClientConfigurationStatus
{
    Missing,
    Invalid,
    Ready
}

// Carries the configuration status and any usable OAuth credentials so Settings can explain connection problems.
public sealed record GoogleOAuthClientConfigurationResult(
    GoogleOAuthClientConfigurationStatus Status,
    ClientSecrets? ClientSecrets,
    string UserMessage)
{
    public static GoogleOAuthClientConfigurationResult Missing() =>
        new(
            GoogleOAuthClientConfigurationStatus.Missing,
            null,
            "Google sign-in app configuration is missing.");

    public static GoogleOAuthClientConfigurationResult Invalid() =>
        new(
            GoogleOAuthClientConfigurationStatus.Invalid,
            null,
            "Google sign-in app configuration is invalid.");

    public static GoogleOAuthClientConfigurationResult Ready(ClientSecrets clientSecrets) =>
        new(
            GoogleOAuthClientConfigurationStatus.Ready,
            clientSecrets,
            "Google sign-in app configuration is ready.");
}

// Reads the desktop OAuth client file and validates it before an authorization flow starts.
public sealed class GoogleOAuthClientConfigurationProvider
{
    private const string ConfigurationFolderName = "Configuration";
    private const string ConfigurationFileName = "google-oauth-client.json";
    private readonly string configurationPath;

    public GoogleOAuthClientConfigurationProvider(string configurationPath)
    {
        this.configurationPath = configurationPath;
    }

    // Resolves the desktop OAuth file beside the app so a source build and installer can use the same layout.
    public static string CreateDefaultConfigurationPath() =>
        Path.Combine(AppContext.BaseDirectory, ConfigurationFolderName, ConfigurationFileName);

    // Loads the desktop OAuth configuration and reports whether Settings can offer a usable Google sign-in.
    public GoogleOAuthClientConfigurationResult Load()
    {
        if (!File.Exists(configurationPath))
        {
            return GoogleOAuthClientConfigurationResult.Missing();
        }

        try
        {
            using (var document = JsonDocument.Parse(File.ReadAllText(configurationPath)))
            {
                if (!document.RootElement.TryGetProperty("installed", out var installedElement) ||
                    installedElement.ValueKind != JsonValueKind.Object)
                {
                    return GoogleOAuthClientConfigurationResult.Invalid();
                }
            }

            using var stream = File.OpenRead(configurationPath);
            var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
            if (string.IsNullOrWhiteSpace(clientSecrets.ClientId) ||
                string.IsNullOrWhiteSpace(clientSecrets.ClientSecret))
            {
                return GoogleOAuthClientConfigurationResult.Invalid();
            }

            return GoogleOAuthClientConfigurationResult.Ready(clientSecrets);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.Text.Json.JsonException or Newtonsoft.Json.JsonException)
        {
            return GoogleOAuthClientConfigurationResult.Invalid();
        }
    }
}
