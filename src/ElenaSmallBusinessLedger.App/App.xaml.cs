using System.Windows;
using ElenaSmallBusinessLedger.App.Services;
using ElenaSmallBusinessLedger.App.ViewModels;

namespace ElenaSmallBusinessLedger.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var viewModel = new MainWindowViewModel(
            new SampleStartupDashboardService(),
            new SampleInvoicePaymentStatusService(),
            new MessageBoxConfirmationService(),
            new ExternalBrowserCalendarLauncher(),
            new SystemUiClock());

        var window = new MainWindow(viewModel);
        MainWindow = window;
        window.Show();
    }
}
