namespace ElenasBusinessLedger.App.Services;

public interface ICalendarLauncher
{
    Task OpenAsync(CancellationToken cancellationToken);
}
