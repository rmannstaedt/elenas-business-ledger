using System.Windows;

namespace ElenasBusinessLedger.App.Services;

public sealed class AboutDialogLauncher : IAboutDialogLauncher
{
    public Task ShowAboutAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var owner = Application.Current?.MainWindow;
        const MessageBoxButton button = MessageBoxButton.OK;
        const MessageBoxImage icon = MessageBoxImage.Information;
        const string caption = ApplicationInfo.AboutMenuText;

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
