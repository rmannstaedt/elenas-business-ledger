using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
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

        return new CalendarService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName
            });
    }
}
