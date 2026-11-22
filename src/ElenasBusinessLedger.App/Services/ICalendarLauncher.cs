namespace ElenasBusinessLedger.App.Services;

// Lets the dashboard request the external calendar without depending on the Windows browser launcher.
public interface ICalendarLauncher
{
    Task OpenAsync(CancellationToken cancellationToken);
}
