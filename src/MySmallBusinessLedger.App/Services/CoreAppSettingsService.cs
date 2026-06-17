using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.App.Services;

public sealed class CoreAppSettingsService : IAppSettingsService
{
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly SqliteCalendarSettingsRepository calendarSettingsRepository;
    private readonly AppPaths appPaths;
    private readonly GoogleOAuthClientConfigurationProvider googleOAuthClientConfigurationProvider;
    private readonly GoogleCalendarClientFactory? googleCalendarClientFactory;

    public CoreAppSettingsService(
        ICompanyProfileRepository companyProfileRepository,
        SqliteCalendarSettingsRepository calendarSettingsRepository,
        string appDataPath)
        : this(
            companyProfileRepository,
            calendarSettingsRepository,
            AppPaths.FromAppDataFolder(appDataPath),
            new GoogleOAuthClientConfigurationProvider(GoogleOAuthClientConfigurationProvider.CreateDefaultConfigurationPath()),
            googleCalendarClientFactory: null)
    {
    }

    public CoreAppSettingsService(
        ICompanyProfileRepository companyProfileRepository,
        SqliteCalendarSettingsRepository calendarSettingsRepository,
        AppPaths appPaths,
        GoogleOAuthClientConfigurationProvider googleOAuthClientConfigurationProvider,
        GoogleCalendarClientFactory? googleCalendarClientFactory = null)
    {
        this.companyProfileRepository = companyProfileRepository;
        this.calendarSettingsRepository = calendarSettingsRepository;
        this.appPaths = appPaths;
        this.googleOAuthClientConfigurationProvider = googleOAuthClientConfigurationProvider;
        this.googleCalendarClientFactory = googleCalendarClientFactory;
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
        return CreateSnapshot(companyProfile, calendarSettings);
    }

    public async Task<OperationResult<AppSettingsSnapshot>> SaveAsync(
        CompanyProfile companyProfile,
        CalendarSettings calendarSettings,
        CancellationToken cancellationToken)
    {
        try
        {
            await companyProfileRepository.SaveAsync(companyProfile, cancellationToken);
            await calendarSettingsRepository.SaveAsync(calendarSettings, cancellationToken);
            return OperationResult<AppSettingsSnapshot>.Success(
                CreateSnapshot(companyProfile, calendarSettings));
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
            var configuredClient = await googleCalendarClientFactory.CreateAsync(calendarSettings, cancellationToken);
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
        CalendarSettings? calendarSettings) =>
        new(
            companyProfile,
            calendarSettings,
            googleOAuthClientConfigurationProvider.Load().Status,
            appPaths.AppDataFolderPath,
            appPaths.DatabasePath,
            appPaths.GoogleTokenFolderPath);
}
