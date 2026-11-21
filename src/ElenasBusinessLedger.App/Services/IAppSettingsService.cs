using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Company;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Infrastructure.Persistence;

namespace ElenasBusinessLedger.App.Services;

// Packages saved preferences and local connection status so Settings can populate the whole form.
public sealed record AppSettingsSnapshot(
    CompanyProfile? CompanyProfile,
    CalendarSettings? CalendarSettings,
    GoogleOAuthClientConfigurationStatus GoogleOAuthAppConfigurationStatus,
    EconomySettings? EconomySettings = null,
    string AppDataFolderPath = "",
    string DatabasePath = "",
    string GoogleTokenFolderPath = "",
    bool HasGoogleAuthorizationTokens = false,
    IReadOnlyList<string>? AvailablePrinterNames = null);

// Gives Settings a contract for saved preferences and explicit Google authorization actions.
public interface IAppSettingsService
{
    Task<AppSettingsSnapshot> LoadAsync(CancellationToken cancellationToken);

    Task<OperationResult<AppSettingsSnapshot>> SaveAsync(
        CompanyProfile companyProfile,
        CalendarSettings calendarSettings,
        EconomySettings? economySettings,
        CancellationToken cancellationToken);

    Task<OperationResult<bool>> ConnectGoogleCalendarAsync(
        CalendarSettings calendarSettings,
        CancellationToken cancellationToken);

    Task<OperationResult<bool>> ForgetGoogleAuthorizationAsync(CancellationToken cancellationToken);
}
