using System.Windows;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.App.Views;

namespace ElenasBusinessLedger.App.Services;

// Opens Settings and returns whether its values and session Admin Mode were saved.
public sealed class SettingsWindowLauncher : ISettingsLauncher
{
    private readonly IAppSettingsService settingsService;
    private readonly IGoogleCalendarAuthorizationService calendarAuthorization;

    public SettingsWindowLauncher(
        IAppSettingsService settingsService, IGoogleCalendarAuthorizationService calendarAuthorization)
    {
        this.settingsService = settingsService;
        this.calendarAuthorization = calendarAuthorization;
    }

    // Opens Settings and passes the saved session Admin Mode back to the dashboard only after a successful save.
    public async Task<SettingsDialogResult> ShowSettingsAsync(bool isAdminMode, CancellationToken cancellationToken)
    {
        var viewModel = new SettingsViewModel(settingsService, calendarAuthorization, isAdminMode);
        await viewModel.LoadAsync();
        var window = new SettingsView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        var wasSaved = window.ShowDialog() == true && viewModel.WasSaved;
        return new SettingsDialogResult(wasSaved, wasSaved ? viewModel.SavedAdminMode : isAdminMode);
    }
}
