using System.Windows;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;

namespace MySmallBusinessLedger.App.Services;

public sealed class SettingsWindowLauncher : ISettingsLauncher
{
    private readonly IAppSettingsService settingsService;

    public SettingsWindowLauncher(IAppSettingsService settingsService)
    {
        this.settingsService = settingsService;
    }

    public async Task<SettingsDialogResult> ShowSettingsAsync(bool isAdminMode, CancellationToken cancellationToken)
    {
        var viewModel = new SettingsViewModel(settingsService, isAdminMode);
        await viewModel.LoadAsync();
        var window = new SettingsView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        var wasSaved = window.ShowDialog() == true && viewModel.WasSaved;
        return new SettingsDialogResult(wasSaved, wasSaved ? viewModel.SavedAdminMode : isAdminMode);
    }
}
