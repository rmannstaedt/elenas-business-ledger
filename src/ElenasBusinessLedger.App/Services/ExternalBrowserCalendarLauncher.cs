using System.Diagnostics;
using ElenasBusinessLedger.Infrastructure.Persistence;

namespace ElenasBusinessLedger.App.Services;

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
