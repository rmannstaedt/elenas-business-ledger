namespace ElenasBusinessLedger.App.Services;

// Tells the dashboard whether Settings were saved and which session Admin Mode to use afterward.
public sealed record SettingsDialogResult(bool WasSaved, bool IsAdminMode);

// Lets the dashboard open Settings and receive the save result without depending on the WPF window.
public interface ISettingsLauncher
{
    Task<SettingsDialogResult> ShowSettingsAsync(bool isAdminMode, CancellationToken cancellationToken);
}
