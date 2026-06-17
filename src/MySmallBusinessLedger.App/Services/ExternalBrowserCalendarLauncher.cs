using System.Diagnostics;

namespace MySmallBusinessLedger.App.Services;

public sealed class ExternalBrowserCalendarLauncher : ICalendarLauncher
{
    private static readonly Uri CalendarUri = new("https://calendar.google.com/calendar/u/0/r");

    public Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Process.Start(new ProcessStartInfo
        {
            FileName = CalendarUri.ToString(),
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }
}
