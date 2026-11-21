using System.Security.Cryptography;
using System.Text;
using ElenasBusinessLedger.Infrastructure.Calendar;
using ElenasBusinessLedger.Infrastructure.Persistence;

namespace ElenasBusinessLedger.App.Services;

internal delegate Task<ConfiguredGoogleCalendarClient?> GoogleCalendarClientBuilder(
    CalendarSettings settings,
    GoogleOAuthClientConfigurationResult configuration,
    WindowsProtectedFileDataStore tokenStore,
    bool allowInteractiveAuthorization,
    CancellationToken cancellationToken);

// Builds a scoped calendar client from Settings and allows interactive authorization only for Connect.
public sealed class GoogleCalendarClientFactory
{
    private readonly Func<CancellationToken, Task<CalendarSettings?>> loadSettingsAsync;
    private readonly GoogleOAuthClientConfigurationProvider configurationProvider;
    private readonly string tokenFolderPath;
    private readonly string applicationName;
    private readonly string timeZoneId;
    private readonly GoogleCalendarClientBuilder buildClientAsync;

    public GoogleCalendarClientFactory(
        Func<CancellationToken, Task<CalendarSettings?>> loadSettingsAsync,
        GoogleOAuthClientConfigurationProvider configurationProvider,
        string tokenFolderPath,
        string applicationName,
        string timeZoneId)
        : this(
            loadSettingsAsync,
            configurationProvider,
            tokenFolderPath,
            applicationName,
            timeZoneId,
            null)
    {
    }

    internal GoogleCalendarClientFactory(
        Func<CancellationToken, Task<CalendarSettings?>> loadSettingsAsync,
        GoogleOAuthClientConfigurationProvider configurationProvider,
        string tokenFolderPath,
        string applicationName,
        string timeZoneId,
        GoogleCalendarClientBuilder? buildClientAsync)
    {
        this.loadSettingsAsync = loadSettingsAsync;
        this.configurationProvider = configurationProvider;
        this.tokenFolderPath = tokenFolderPath;
        this.applicationName = applicationName;
        this.timeZoneId = timeZoneId;
        this.buildClientAsync = buildClientAsync ?? BuildClientAsync;
    }

    // Opens the saved calendar with existing credentials so background refresh does not start an authorization prompt.
    public async Task<ConfiguredGoogleCalendarClient?> CreateAsync(CancellationToken cancellationToken)
    {
        var settings = await loadSettingsAsync(cancellationToken);
        if (settings is null ||
            string.IsNullOrWhiteSpace(settings.SelectedCalendarId) ||
            string.IsNullOrWhiteSpace(settings.GoogleAccountEmail))
        {
            return null;
        }

        return await CreateAsync(settings, allowInteractiveAuthorization: false, cancellationToken);
    }

    // Uses the supplied calendar settings with saved credentials and leaves interactive authorization to Connect.
    public async Task<ConfiguredGoogleCalendarClient?> CreateAsync(
        CalendarSettings settings,
        CancellationToken cancellationToken) =>
        await CreateAsync(settings, allowInteractiveAuthorization: false, cancellationToken);

    // Allows interactive Google sign-in during the explicit Connect action in Settings.
    public async Task<ConfiguredGoogleCalendarClient?> CreateForConnectionAsync(
        CalendarSettings settings,
        CancellationToken cancellationToken) =>
        await CreateAsync(settings, allowInteractiveAuthorization: true, cancellationToken);

    private async Task<ConfiguredGoogleCalendarClient?> CreateAsync(
        CalendarSettings settings,
        bool allowInteractiveAuthorization,
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
        if (!allowInteractiveAuthorization &&
            !tokenStore.ContainsKey(settings.GoogleAccountEmail))
        {
            return null;
        }

        return await buildClientAsync(
            settings,
            configuration,
            tokenStore,
            allowInteractiveAuthorization,
            cancellationToken);
    }

    internal static string CreateCalendarScope(CalendarSettings settings)
    {
        var calendarId = settings.SelectedCalendarId.Trim();
        var identity = string.Equals(calendarId, "primary", StringComparison.Ordinal)
            ? settings.GoogleAccountEmail.Trim().ToLowerInvariant()
            : calendarId;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    private async Task<ConfiguredGoogleCalendarClient?> BuildClientAsync(
        CalendarSettings settings,
        GoogleOAuthClientConfigurationResult configuration,
        WindowsProtectedFileDataStore tokenStore,
        bool allowInteractiveAuthorization,
        CancellationToken cancellationToken)
    {
        var credentialFactory = new GoogleCalendarCredentialFactory(
            configuration.ClientSecrets!,
            tokenStore,
            applicationName);
        var calendarService = allowInteractiveAuthorization
            ? await credentialFactory.CreateCalendarServiceAsync(
                settings.GoogleAccountEmail,
                cancellationToken)
            : await credentialFactory.TryCreateCalendarServiceFromSavedCredentialAsync(
                settings.GoogleAccountEmail,
                cancellationToken);

        return calendarService is null
            ? null
            : new ConfiguredGoogleCalendarClient(
                settings.SelectedCalendarId,
                new GoogleCalendarServiceEventClient(calendarService, timeZoneId),
                CreateCalendarScope(settings));
    }
}
