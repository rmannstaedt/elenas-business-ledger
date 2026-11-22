namespace ElenasBusinessLedger.App.Services;

// Lets the dashboard request the Economy window with the current session's editing mode.
public interface IEconomyOverviewLauncher
{
    Task ShowEconomyOverviewAsync(bool isAdminMode, CancellationToken cancellationToken);
}
