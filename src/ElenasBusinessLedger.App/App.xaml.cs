using System.Windows;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.Startup;

namespace ElenasBusinessLedger.App;

// Starts the WPF application and hands initialization to the bootstrapper before showing the main window.
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplicationStartupResult startupResult;
        try
        {
            startupResult = await new ApplicationBootstrapper().StartAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Application startup failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        foreach (var warning in startupResult.Warnings)
        {
            MessageBox.Show(
                warning.Message,
                warning.Title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        MainWindow = startupResult.MainWindow;
        if (AppPaths.IsUserTestLaunch)
        {
            MainWindow.Title = $"{MainWindow.Title} [TEST DATA]";
        }

        MainWindow.Show();
    }
}
