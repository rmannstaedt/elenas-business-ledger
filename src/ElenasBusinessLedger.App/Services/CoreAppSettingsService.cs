using System.IO;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Company;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Infrastructure.Invoices.Printing;
using ElenasBusinessLedger.Infrastructure.Persistence;

namespace ElenasBusinessLedger.App.Services;

public sealed class CoreAppSettingsService : IAppSettingsService
{
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly SqliteCalendarSettingsRepository calendarSettingsRepository;
    private readonly SqliteAppSettingsWriter settingsWriter;
    private readonly IEconomyRepository? economyRepository;
    private readonly AppPaths appPaths;
    private readonly GoogleOAuthClientConfigurationProvider googleOAuthClientConfigurationProvider;
    private readonly GoogleCalendarClientFactory? googleCalendarClientFactory;
    private readonly IPrinterCatalog printerCatalog;

    public CoreAppSettingsService(
        ICompanyProfileRepository companyProfileRepository,
        SqliteCalendarSettingsRepository calendarSettingsRepository,
        string appDataPath,
        IEconomyRepository? economyRepository = null)
        : this(
            companyProfileRepository,
            calendarSettingsRepository,
            AppPaths.FromAppDataFolder(appDataPath),
            new GoogleOAuthClientConfigurationProvider(GoogleOAuthClientConfigurationProvider.CreateDefaultConfigurationPath()),
            googleCalendarClientFactory: null,
            economyRepository: economyRepository,
            printerCatalog: new WindowsPrinterCatalog())
    {
    }

    public CoreAppSettingsService(
        ICompanyProfileRepository companyProfileRepository,
        SqliteCalendarSettingsRepository calendarSettingsRepository,
        AppPaths appPaths,
        GoogleOAuthClientConfigurationProvider googleOAuthClientConfigurationProvider,
        GoogleCalendarClientFactory? googleCalendarClientFactory = null,
        IEconomyRepository? economyRepository = null,
        IPrinterCatalog? printerCatalog = null)
    {
        this.companyProfileRepository = companyProfileRepository;
        this.calendarSettingsRepository = calendarSettingsRepository;
        settingsWriter = calendarSettingsRepository.CreateSettingsWriter();
        this.economyRepository = economyRepository;
        this.appPaths = appPaths;
        this.googleOAuthClientConfigurationProvider = googleOAuthClientConfigurationProvider;
        this.googleCalendarClientFactory = googleCalendarClientFactory;
        this.printerCatalog = printerCatalog ?? new WindowsPrinterCatalog();
    }

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

        var calendarSettings = await calendarSettingsRepository.GetAsync(cancellationToken);
        var economySettings = economyRepository is null
            ? null
            : await economyRepository.GetSettingsAsync(cancellationToken);
        var printerNames = await LoadPrinterNamesAsync(cancellationToken);
        return CreateSnapshot(companyProfile, calendarSettings, economySettings, printerNames);
    }

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

    public async Task<OperationResult<bool>> ConnectGoogleCalendarAsync(
        CalendarSettings calendarSettings,
        CancellationToken cancellationToken)
    {
        if (googleCalendarClientFactory is null)
        {
            return OperationResult<bool>.Failure(
                new ValidationError("Settings.GoogleCalendarUnavailable", "Google Calendar connection is not available."));
        }

        var configuration = googleOAuthClientConfigurationProvider.Load();
        if (configuration.Status == GoogleOAuthClientConfigurationStatus.Missing)
        {
            return OperationResult<bool>.Failure(
                new ValidationError("Settings.GoogleOAuthMissing", "Google sign-in app configuration is missing."));
        }

        if (configuration.Status == GoogleOAuthClientConfigurationStatus.Invalid)
        {
            return OperationResult<bool>.Failure(
                new ValidationError("Settings.GoogleOAuthInvalid", "Google sign-in app configuration is invalid."));
        }

        try
        {
            var configuredClient = await googleCalendarClientFactory.CreateForConnectionAsync(calendarSettings, cancellationToken);
            if (configuredClient is null)
            {
                return OperationResult<bool>.Failure(
                    new ValidationError("Settings.GoogleCalendarUnavailable", "Google Calendar connection is not available."));
            }

            var now = DateTimeOffset.Now;
            var fromInclusive = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
            var toExclusive = fromInclusive.AddMonths(1);
            await configuredClient.EventClient.ListEventsAsync(
                configuredClient.CalendarId,
                fromInclusive,
                toExclusive,
                cancellationToken);

            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<bool>.Failure(
                new ValidationError("Settings.GoogleCalendarConnectionFailed", "Google Calendar connection failed."));
        }
    }

    public async Task<OperationResult<bool>> ForgetGoogleAuthorizationAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tokenStore = new Infrastructure.Calendar.WindowsProtectedFileDataStore(appPaths.GoogleTokenFolderPath);
            await tokenStore.ClearAsync();
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<bool>.Failure(
                new ValidationError("Settings.GoogleAuthorizationClearFailed", "Google authorization could not be cleared."));
        }
    }

    private AppSettingsSnapshot CreateSnapshot(
        CompanyProfile? companyProfile,
        CalendarSettings? calendarSettings,
        EconomySettings? economySettings,
        IReadOnlyList<string>? availablePrinterNames = null) =>
        new(
            companyProfile,
            calendarSettings,
            googleOAuthClientConfigurationProvider.Load().Status,
            economySettings,
            appPaths.AppDataFolderPath,
            appPaths.DatabasePath,
            appPaths.GoogleTokenFolderPath,
            HasGoogleAuthorizationTokens(calendarSettings),
            availablePrinterNames ?? []);

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

    private bool HasGoogleAuthorizationTokens(CalendarSettings? calendarSettings)
    {
        if (calendarSettings is null ||
            string.IsNullOrWhiteSpace(calendarSettings.GoogleAccountEmail) ||
            !Directory.Exists(appPaths.GoogleTokenFolderPath))
        {
            return false;
        }

        var tokenStore = new Infrastructure.Calendar.WindowsProtectedFileDataStore(appPaths.GoogleTokenFolderPath);
        return tokenStore.ContainsKey(calendarSettings.GoogleAccountEmail);
    }
}
