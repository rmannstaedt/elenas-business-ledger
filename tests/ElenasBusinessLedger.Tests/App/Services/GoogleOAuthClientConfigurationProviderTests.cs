using ElenasBusinessLedger.App.Services;

namespace ElenasBusinessLedger.Tests.App.Services;

public sealed class GoogleOAuthClientConfigurationProviderTests
{
    [Fact]
    public void LoadReturnsReadyForDesktopOAuthClientJson()
    {
        var path = WriteJson("""
            {
              "installed": {
                "client_id": "fake-client-id.apps.googleusercontent.com",
                "project_id": "my-small-business-ledger",
                "auth_uri": "https://accounts.google.com/o/oauth2/auth",
                "token_uri": "https://oauth2.googleapis.com/token",
                "client_secret": "fake-client-secret"
              }
            }
            """);
        var provider = new GoogleOAuthClientConfigurationProvider(path);

        var result = provider.Load();

        Assert.Equal(GoogleOAuthClientConfigurationStatus.Ready, result.Status);
        Assert.NotNull(result.ClientSecrets);
        Assert.Equal("fake-client-id.apps.googleusercontent.com", result.ClientSecrets.ClientId);
        Assert.Equal("fake-client-secret", result.ClientSecrets.ClientSecret);
    }

    [Fact]
    public void LoadReturnsMissingWhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), "ElenasBusinessLedgerOAuthTests", Guid.NewGuid().ToString("N"), "google-oauth-client.json");
        var provider = new GoogleOAuthClientConfigurationProvider(path);

        var result = provider.Load();

        Assert.Equal(GoogleOAuthClientConfigurationStatus.Missing, result.Status);
        Assert.Null(result.ClientSecrets);
        Assert.Contains("missing", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadReturnsInvalidForMalformedJsonWithoutLeakingSecret()
    {
        var path = WriteJson("""
            {
              "installed": {
                "client_id": "fake-client-id.apps.googleusercontent.com",
                "client_secret": "do-not-repeat-this-secret"
              }
            """);
        var provider = new GoogleOAuthClientConfigurationProvider(path);

        var result = provider.Load();

        Assert.Equal(GoogleOAuthClientConfigurationStatus.Invalid, result.Status);
        Assert.Null(result.ClientSecrets);
        Assert.DoesNotContain("do-not-repeat-this-secret", result.UserMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadReturnsInvalidWhenDesktopClientFieldsAreMissing()
    {
        var path = WriteJson("""
            {
              "installed": {
                "project_id": "my-small-business-ledger"
              }
            }
            """);
        var provider = new GoogleOAuthClientConfigurationProvider(path);

        var result = provider.Load();

        Assert.Equal(GoogleOAuthClientConfigurationStatus.Invalid, result.Status);
        Assert.Null(result.ClientSecrets);
    }

    [Fact]
    public void LoadReturnsInvalidForWebOAuthClientJson()
    {
        var path = WriteJson("""
            {
              "web": {
                "client_id": "fake-web-client-id.apps.googleusercontent.com",
                "project_id": "my-small-business-ledger",
                "auth_uri": "https://accounts.google.com/o/oauth2/auth",
                "token_uri": "https://oauth2.googleapis.com/token",
                "client_secret": "fake-web-client-secret"
              }
            }
            """);
        var provider = new GoogleOAuthClientConfigurationProvider(path);

        var result = provider.Load();

        Assert.Equal(GoogleOAuthClientConfigurationStatus.Invalid, result.Status);
        Assert.Null(result.ClientSecrets);
    }

    private static string WriteJson(string json)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ElenasBusinessLedgerOAuthTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "google-oauth-client.json");
        File.WriteAllText(path, json);
        return path;
    }
}
