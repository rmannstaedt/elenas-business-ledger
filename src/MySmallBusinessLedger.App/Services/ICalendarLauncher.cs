namespace MySmallBusinessLedger.App.Services;

public interface ICalendarLauncher
{
    Task OpenAsync(CancellationToken cancellationToken);
}
