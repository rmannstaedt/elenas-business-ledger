using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Company;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Infrastructure.Invoices.Printing;

namespace ElenasBusinessLedger.App.Services;

// Assembles the Settings snapshot and saves preferences through the atomic settings writer.
public sealed class CoreAppSettingsService(
    ICompanyProfileRepository companyProfileRepository,
    Func<CancellationToken, Task<CalendarSettings?>> loadCalendarSettingsAsync,
    IAppSettingsWriter settingsWriter,
    AppPaths appPaths,
    Func<CalendarSettings?, GoogleCalendarAuthorizationStatus> readAuthorizationStatus,
    IPrinterCatalog printerCatalog,
    IEconomyRepository? economyRepository = null) : IAppSettingsService
{
    // Collects saved settings and local printer and authorization information for the Settings form.
    public async Task<AppSettingsSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        CompanyProfile? companyProfile = null;
        try
        {
            companyProfile = await companyProfileRepository.GetAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            companyProfile = null;
        }

        var calendarSettings = await loadCalendarSettingsAsync(cancellationToken);
        var economySettings = economyRepository is null
            ? null
            : await economyRepository.GetSettingsAsync(cancellationToken);
        var printerNames = await LoadPrinterNamesAsync(cancellationToken);
        return CreateSnapshot(companyProfile, calendarSettings, economySettings, printerNames);
    }

    // Saves company, calendar, and Economy settings together and returns the snapshot shown in Settings.
    public async Task<OperationResult<AppSettingsSnapshot>> SaveAsync(
        CompanyProfile companyProfile,
        CalendarSettings calendarSettings,
        EconomySettings? economySettings,
        CancellationToken cancellationToken)
    {
        try
        {
            if (economySettings is not null && economyRepository is null)
            {
                return OperationResult<AppSettingsSnapshot>.Failure(
                    new ValidationError("Settings.EconomySettingsUnavailable", "Economy settings cannot be saved."));
            }

            var currentEconomySettings = await settingsWriter.SaveAsync(companyProfile, calendarSettings, economySettings, cancellationToken);
            return OperationResult<AppSettingsSnapshot>.Success(
                CreateSnapshot(companyProfile, calendarSettings, currentEconomySettings));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<AppSettingsSnapshot>.Failure(
                new ValidationError("Settings.SaveFailed", ex.Message));
        }
    }

    private AppSettingsSnapshot CreateSnapshot(
        CompanyProfile? companyProfile,
        CalendarSettings? calendarSettings,
        EconomySettings? economySettings,
        IReadOnlyList<string>? availablePrinterNames = null)
    {
        var authorization = readAuthorizationStatus(calendarSettings);
        return new AppSettingsSnapshot(
            companyProfile,
            calendarSettings,
            authorization.ConfigurationStatus,
            economySettings,
            appPaths.AppDataFolderPath,
            appPaths.DatabasePath,
            appPaths.GoogleTokenFolderPath,
            authorization.HasSavedToken,
            availablePrinterNames ?? []);
    }

    private async Task<IReadOnlyList<string>> LoadPrinterNamesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await printerCatalog.ListPrinterNamesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return [];
        }
    }
}
