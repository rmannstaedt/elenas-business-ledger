using MySmallBusinessLedger.App.Services;
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

    private static string MissingConfigurationPath() =>
        Path.Combine(Path.GetTempPath(), "MySmallBusinessLedgerOAuthTests", Guid.NewGuid().ToString("N"), "google-oauth-client.json");
}
