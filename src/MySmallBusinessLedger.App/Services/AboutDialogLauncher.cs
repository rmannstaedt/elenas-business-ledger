using System.Windows;

namespace MySmallBusinessLedger.App.Services;

public sealed class AboutDialogLauncher : IAboutDialogLauncher
{
    public Task ShowAboutAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var owner = Application.Current?.MainWindow;
        const MessageBoxButton button = MessageBoxButton.OK;
        const MessageBoxImage icon = MessageBoxImage.Information;
        var caption = $"About {ApplicationInfo.ProductName}";

        if (owner is null)
        {
            MessageBox.Show(ApplicationInfo.AboutText, caption, button, icon);
        }
        else
        {
            MessageBox.Show(owner, ApplicationInfo.AboutText, caption, button, icon);
        }

        return Task.CompletedTask;
    }
}
