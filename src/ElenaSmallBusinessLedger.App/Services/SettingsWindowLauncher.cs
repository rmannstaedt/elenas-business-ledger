using System.Windows;
using ElenaSmallBusinessLedger.App.ViewModels;
using ElenaSmallBusinessLedger.App.Views;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class SettingsWindowLauncher : ISettingsLauncher
{
    private readonly IAppSettingsService settingsService;

    public SettingsWindowLauncher(IAppSettingsService settingsService)
    {
        this.settingsService = settingsService;
    }

    public async Task<bool> ShowSettingsAsync(CancellationToken cancellationToken)
    {
        var viewModel = new SettingsViewModel(settingsService);
        await viewModel.LoadAsync();
        var window = new SettingsView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        return window.ShowDialog() == true && viewModel.WasSaved;
    }
}
