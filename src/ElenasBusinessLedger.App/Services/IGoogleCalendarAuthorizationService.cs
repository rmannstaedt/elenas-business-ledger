using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;

namespace ElenasBusinessLedger.App.Services;

// Provides the explicit sign-in and forget actions shown in the Settings window.
public interface IGoogleCalendarAuthorizationService
{
    Task<OperationResult<bool>> ConnectGoogleCalendarAsync(
        CalendarSettings calendarSettings, CancellationToken cancellationToken);

    Task<OperationResult<bool>> ForgetGoogleAuthorizationAsync(CancellationToken cancellationToken);
}
