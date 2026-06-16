using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace ElenaSmallBusinessLedger.Infrastructure.Calendar;

public sealed class GoogleCalendarCredentialFactory
{
    private static readonly string[] Scopes = [CalendarService.Scope.CalendarEvents];
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
            Scopes,
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
