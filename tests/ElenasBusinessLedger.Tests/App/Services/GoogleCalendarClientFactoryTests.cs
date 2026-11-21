using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Infrastructure.Calendar;
using ElenasBusinessLedger.Infrastructure.Persistence;

namespace ElenasBusinessLedger.Tests.App.Services;

// Checks when the factory can reuse authorization, request sign-in, or return no calendar client.
public sealed class GoogleCalendarClientFactoryTests
{
    [Fact]
    public async Task CreateAsyncReturnsNullWhenCalendarSettingsAreMissing()
    {
        var factory = new GoogleCalendarClientFactory(
            _ => Task.FromResult<CalendarSettings?>(null),
            new GoogleOAuthClientConfigurationProvider(MissingConfigurationPath()),
            "token-folder",
            "Elena's Business Ledger",
            TimeZoneInfo.Local.Id);

        var client = await factory.CreateAsync(CancellationToken.None);

        Assert.Null(client);
    }

    [Fact]
    public async Task CreateAsyncReturnsNullWhenOAuthAppConfigurationIsMissing()
    {
        var factory = new GoogleCalendarClientFactory(
            _ => Task.FromResult<CalendarSettings?>(new CalendarSettings("primary", "elena@example.test")),
            new GoogleOAuthClientConfigurationProvider(MissingConfigurationPath()),
            "token-folder",
            "Elena's Business Ledger",
            TimeZoneInfo.Local.Id);

        var client = await factory.CreateAsync(CancellationToken.None);

        Assert.Null(client);
    }

    [Fact]
    public async Task CreateAsyncReturnsNullWithoutInvokingAuthorizationWhenSavedTokenIsMissing()
    {
        using var directory = new TemporaryDirectory();
        var configurationPath = WriteOAuthConfiguration(directory.Path);
        var authorizationWasInvoked = false;
        var factory = new GoogleCalendarClientFactory(
            _ => Task.FromResult<CalendarSettings?>(new CalendarSettings("primary", "elena@example.test")),
            new GoogleOAuthClientConfigurationProvider(configurationPath),
            Path.Combine(directory.Path, "GoogleTokens"),
            "Elena's Business Ledger",
            TimeZoneInfo.Local.Id,
            (settings, configuration, tokenStore, allowInteractiveAuthorization, cancellationToken) =>
            {
                authorizationWasInvoked = true;
                return Task.FromResult<ConfiguredGoogleCalendarClient?>(null);
            });

        var client = await factory.CreateAsync(CancellationToken.None);

        Assert.Null(client);
        Assert.False(authorizationWasInvoked);
    }

    [Fact]
    public async Task CreateForConnectionAsyncInvokesInteractiveAuthorizationWithoutSavedToken()
    {
        using var directory = new TemporaryDirectory();
        var configurationPath = WriteOAuthConfiguration(directory.Path);
        var authorizationWasInvoked = false;
        var sawInteractiveAuthorization = false;
        var eventClient = new FakeGoogleCalendarEventClient();
        var factory = new GoogleCalendarClientFactory(
            _ => Task.FromResult<CalendarSettings?>(null),
            new GoogleOAuthClientConfigurationProvider(configurationPath),
            Path.Combine(directory.Path, "GoogleTokens"),
            "Elena's Business Ledger",
            TimeZoneInfo.Local.Id,
            (settings, configuration, tokenStore, allowInteractiveAuthorization, cancellationToken) =>
            {
                authorizationWasInvoked = true;
                sawInteractiveAuthorization = allowInteractiveAuthorization;
                return Task.FromResult<ConfiguredGoogleCalendarClient?>(
                    new ConfiguredGoogleCalendarClient(settings.SelectedCalendarId, eventClient));
            });

        var client = await factory.CreateForConnectionAsync(
            new CalendarSettings("primary", "elena@example.test"),
            CancellationToken.None);

        Assert.NotNull(client);
        Assert.True(authorizationWasInvoked);
        Assert.True(sawInteractiveAuthorization);
    }

    private static string MissingConfigurationPath() =>
        Path.Combine(Path.GetTempPath(), "ElenasBusinessLedgerOAuthTests", Guid.NewGuid().ToString("N"), "google-oauth-client.json");

    private static string WriteOAuthConfiguration(string directory)
    {
        var configurationPath = Path.Combine(directory, "google-oauth-client.json");
        File.WriteAllText(
            configurationPath,
            """
            {
              "installed": {
                "client_id": "client-id.apps.googleusercontent.com",
                "client_secret": "client-secret",
                "auth_uri": "https://accounts.google.com/o/oauth2/auth",
                "token_uri": "https://oauth2.googleapis.com/token"
              }
            }
            """);
        return configurationPath;
    }

    // Isolates the OAuth configuration and token files used to test calendar client creation.
    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ElenasBusinessLedgerOAuthTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }

    // Stands in for an authorized event client so factory tests can inspect setup without Google requests.
    private sealed class FakeGoogleCalendarEventClient : IGoogleCalendarEventClient
    {
        public Task<IReadOnlyList<GoogleCalendarEvent>> ListEventsAsync(
            string calendarId,
            DateTimeOffset fromInclusive,
            DateTimeOffset toExclusive,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GoogleCalendarEvent>>([]);

        public Task<string> CreateLessonEventAsync(
            string calendarId,
            string summary,
            DateTimeOffset startsAt,
            bool hasKnownStartTime,
            TimeSpan duration,
            CancellationToken cancellationToken) =>
            Task.FromResult("created");

        public Task MoveEventAsync(
            string calendarId,
            string eventId,
            DateTimeOffset startsAt,
            bool hasKnownStartTime,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DeleteEventAsync(
            string calendarId,
            string eventId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task UpdateLessonStateAsync(
            string calendarId,
            string eventId,
            ElenasBusinessLedger.Core.Lessons.LessonState state,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
