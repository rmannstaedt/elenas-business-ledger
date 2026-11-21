using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Infrastructure.Persistence;

namespace ElenasBusinessLedger.Tests.App.Services;

public sealed class ExternalBrowserCalendarLauncherTests
{
    [Fact]
    public async Task OpenAsyncHintsConfiguredGoogleAccountToGoogleCalendar()
    {
        Uri? openedUri = null;
        var launcher = new ExternalBrowserCalendarLauncher(
            _ => Task.FromResult<CalendarSettings?>(new CalendarSettings("primary", "studio@example.test")),
            uri => openedUri = uri);

        await launcher.OpenAsync(CancellationToken.None);

        Assert.NotNull(openedUri);
        Assert.Equal("https", openedUri.Scheme);
        Assert.Equal("calendar.google.com", openedUri.Host);
        Assert.Equal("/calendar/r", openedUri.AbsolutePath);
        Assert.Contains("authuser=studio%40example.test", openedUri.Query);
    }

    [Fact]
    public async Task OpenAsyncFallsBackToGenericGoogleCalendarWhenAccountEmailIsMissing()
    {
        Uri? openedUri = null;
        var launcher = new ExternalBrowserCalendarLauncher(
            _ => Task.FromResult<CalendarSettings?>(new CalendarSettings("primary", "")),
            uri => openedUri = uri);

        await launcher.OpenAsync(CancellationToken.None);

        Assert.NotNull(openedUri);
        Assert.Equal("https://calendar.google.com/calendar/r", openedUri.ToString());
    }
}
