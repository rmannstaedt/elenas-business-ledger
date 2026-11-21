using System.Windows;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.App.Views;

namespace ElenasBusinessLedger.App.Services;

// Opens Settings and returns whether its values and session Admin Mode were saved.
public sealed class SettingsWindowLauncher : ISettingsLauncher
{
    private readonly IAppSettingsService settingsService;

    public SettingsWindowLauncher(IAppSettingsService settingsService)
    {
        this.settingsService = settingsService;
    }

    // Opens Settings and passes the saved session Admin Mode back to the dashboard only after a successful save.
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
