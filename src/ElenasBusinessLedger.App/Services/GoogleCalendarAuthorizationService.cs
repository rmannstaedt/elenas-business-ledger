using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;

namespace ElenasBusinessLedger.App.Services;

// Carries the two read-only Google sign-in indicators displayed alongside saved preferences.
public sealed record GoogleCalendarAuthorizationStatus(
    GoogleOAuthClientConfigurationStatus ConfigurationStatus, bool HasSavedToken);

// Owns explicit Google sign-in and token clearing so preference saving stays independent of authorization.
public sealed class GoogleCalendarAuthorizationService(
    GoogleOAuthClientConfigurationProvider configurationProvider,
    Func<CalendarSettings, CancellationToken, Task<ConfiguredGoogleCalendarClient?>> connectCalendarAsync,
    Func<string, bool> hasSavedToken,
    Func<Task> clearSavedTokensAsync) : IGoogleCalendarAuthorizationService
{
    // Reports local authorization status without starting a browser connection.
    public GoogleCalendarAuthorizationStatus GetStatus(CalendarSettings? settings) =>
        new(configurationProvider.Load().Status,
            !string.IsNullOrWhiteSpace(settings?.GoogleAccountEmail) && hasSavedToken(settings.GoogleAccountEmail));

    // Starts explicit authorization and reads the selected calendar to check that the connection works.
    public async Task<OperationResult<bool>> ConnectGoogleCalendarAsync(
        CalendarSettings calendarSettings,
        CancellationToken cancellationToken)
    {
        var configuration = configurationProvider.Load();
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
            var configuredClient = await connectCalendarAsync(calendarSettings, cancellationToken);
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

    // Clears the Ledger's saved Google tokens for this Windows user so Connect can authorize again.
    public async Task<OperationResult<bool>> ForgetGoogleAuthorizationAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await clearSavedTokensAsync();
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<bool>.Failure(
                new ValidationError("Settings.GoogleAuthorizationClearFailed", "Google authorization could not be cleared."));
        }
    }
}
