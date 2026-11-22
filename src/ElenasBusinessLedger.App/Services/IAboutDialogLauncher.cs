namespace ElenasBusinessLedger.App.Services;

// Lets the dashboard request the About display without constructing WPF UI in the view model.
public interface IAboutDialogLauncher
{
    Task ShowAboutAsync(CancellationToken cancellationToken);
}
