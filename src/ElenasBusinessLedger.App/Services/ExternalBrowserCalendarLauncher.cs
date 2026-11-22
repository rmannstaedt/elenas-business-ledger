using System.Diagnostics;
using ElenasBusinessLedger.Core.Calendar;

namespace ElenasBusinessLedger.App.Services;

// Opens Google Calendar in the user's browser with an account hint from the saved settings.
public sealed class ExternalBrowserCalendarLauncher : ICalendarLauncher
{
    private readonly Func<CancellationToken, Task<CalendarSettings?>> loadCalendarSettingsAsync;
    private readonly Action<Uri> openUri;

    public ExternalBrowserCalendarLauncher()
        : this(_ => Task.FromResult<CalendarSettings?>(null))
    {
    }

    public ExternalBrowserCalendarLauncher(
        Func<CancellationToken, Task<CalendarSettings?>> loadCalendarSettingsAsync,
        Action<Uri>? openUri = null)
    {
        this.loadCalendarSettingsAsync = loadCalendarSettingsAsync;
        this.openUri = openUri ?? OpenWithShell;
    }

    // Opens Google Calendar in the browser, using the configured account as a sign-in hint when available.
    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var calendarSettings = await loadCalendarSettingsAsync(cancellationToken);
        var calendarUri = BuildCalendarUri(calendarSettings);
        openUri(calendarUri);
    }

    private static Uri BuildCalendarUri(CalendarSettings? calendarSettings)
    {
        if (string.IsNullOrWhiteSpace(calendarSettings?.GoogleAccountEmail))
        {
            return new Uri("https://calendar.google.com/calendar/r");
        }

        var builder = new UriBuilder("https://calendar.google.com/calendar/r")
        {
            Query = "authuser=" + Uri.EscapeDataString(calendarSettings.GoogleAccountEmail.Trim())
        };
        return builder.Uri;
    }

    private static void OpenWithShell(Uri calendarUri)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = calendarUri.ToString(),
            UseShellExecute = true
        });
    }
}
