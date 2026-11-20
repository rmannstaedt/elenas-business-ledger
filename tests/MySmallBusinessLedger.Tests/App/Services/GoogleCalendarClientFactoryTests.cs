using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Infrastructure.Calendar;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.Tests.App.Services;

public sealed class GoogleCalendarClientFactoryTests
{
    [Fact]
    public async Task CreateAsyncReturnsNullWhenCalendarSettingsAreMissing()
    {
        var factory = new GoogleCalendarClientFactory(
            _ => Task.FromResult<CalendarSettings?>(null),
            new GoogleOAuthClientConfigurationProvider(MissingConfigurationPath()),
            "token-folder",
            "My Small Business Ledger",
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
            "My Small Business Ledger",
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
            "My Small Business Ledger",
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
            "My Small Business Ledger",
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
        Path.Combine(Path.GetTempPath(), "MySmallBusinessLedgerOAuthTests", Guid.NewGuid().ToString("N"), "google-oauth-client.json");

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

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MySmallBusinessLedgerOAuthTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }

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
            MySmallBusinessLedger.Core.Lessons.LessonState state,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
