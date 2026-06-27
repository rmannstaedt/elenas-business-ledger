using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Calendar.v3;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace MySmallBusinessLedger.Infrastructure.Calendar;

public sealed class GoogleCalendarCredentialFactory
{
    internal static readonly string[] RequestedScopes = [CalendarService.Scope.CalendarEventsOwned];
    private readonly ClientSecrets clientSecrets;
    private readonly IDataStore tokenStore;
    private readonly string applicationName;

    public GoogleCalendarCredentialFactory(
        ClientSecrets clientSecrets,
        IDataStore tokenStore,
        string applicationName)
    {
        this.clientSecrets = clientSecrets;
        this.tokenStore = tokenStore;
        this.applicationName = applicationName;
    }

    public async Task<CalendarService> CreateCalendarServiceAsync(string userId, CancellationToken cancellationToken)
    {
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets,
            RequestedScopes,
            userId,
            cancellationToken,
            tokenStore);

        return CreateCalendarService(credential);
    }

    public async Task<CalendarService?> TryCreateCalendarServiceFromSavedCredentialAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var flow = CreateAuthorizationCodeFlow();
        var token = await flow.LoadTokenAsync(userId, cancellationToken);
        if (token is null)
        {
            return null;
        }

        return CreateCalendarService(new UserCredential(flow, userId, token));
    }

    private GoogleAuthorizationCodeFlow CreateAuthorizationCodeFlow() =>
        new(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = RequestedScopes,
                DataStore = tokenStore
            });

    private CalendarService CreateCalendarService(IConfigurableHttpClientInitializer credential) =>
        new(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName
            });
}
