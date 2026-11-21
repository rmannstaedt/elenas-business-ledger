using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Company;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Infrastructure.Persistence;

namespace ElenasBusinessLedger.App.Services;

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
