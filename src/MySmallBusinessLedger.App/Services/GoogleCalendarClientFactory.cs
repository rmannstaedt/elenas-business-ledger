using MySmallBusinessLedger.Infrastructure.Calendar;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.App.Services;

public sealed class GoogleCalendarClientFactory
{
    private readonly Func<CancellationToken, Task<CalendarSettings?>> loadSettingsAsync;
    private readonly GoogleOAuthClientConfigurationProvider configurationProvider;
    private readonly string tokenFolderPath;
    private readonly string applicationName;
    private readonly string timeZoneId;

    public GoogleCalendarClientFactory(
        Func<CancellationToken, Task<CalendarSettings?>> loadSettingsAsync,
        GoogleOAuthClientConfigurationProvider configurationProvider,
        string tokenFolderPath,
        string applicationName,
        string timeZoneId)
    {
        this.loadSettingsAsync = loadSettingsAsync;
        this.configurationProvider = configurationProvider;
        this.tokenFolderPath = tokenFolderPath;
        this.applicationName = applicationName;
        this.timeZoneId = timeZoneId;
    }

    public async Task<ConfiguredGoogleCalendarClient?> CreateAsync(CancellationToken cancellationToken)
    {
        var settings = await loadSettingsAsync(cancellationToken);
        if (settings is null ||
            string.IsNullOrWhiteSpace(settings.SelectedCalendarId) ||
            string.IsNullOrWhiteSpace(settings.GoogleAccountEmail))
        {
            return null;
        }

        return await CreateAsync(settings, cancellationToken);
    }

    public async Task<ConfiguredGoogleCalendarClient?> CreateAsync(
        CalendarSettings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.SelectedCalendarId) ||
            string.IsNullOrWhiteSpace(settings.GoogleAccountEmail))
        {
            return null;
        }

        var configuration = configurationProvider.Load();
        if (configuration.Status != GoogleOAuthClientConfigurationStatus.Ready ||
            configuration.ClientSecrets is null)
        {
            return null;
        }

        var tokenStore = new WindowsProtectedFileDataStore(tokenFolderPath);
        var credentialFactory = new GoogleCalendarCredentialFactory(
            configuration.ClientSecrets,
            tokenStore,
            applicationName);
        var calendarService = await credentialFactory.CreateCalendarServiceAsync(
            settings.GoogleAccountEmail,
            cancellationToken);

        return new ConfiguredGoogleCalendarClient(
            settings.SelectedCalendarId,
            new GoogleCalendarServiceEventClient(calendarService, timeZoneId));
    }
}
