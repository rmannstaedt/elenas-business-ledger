using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Calendar.v3;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace ElenasBusinessLedger.Infrastructure.Calendar;

// Builds a Calendar API service from saved credentials or explicit sign-in so refresh can run without a prompt.
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

    // Authorizes the selected Google user and opens a calendar client for the explicit connection workflow.
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

    // Creates a client from a saved token so dashboard refresh can proceed without starting interactive sign-in.
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
