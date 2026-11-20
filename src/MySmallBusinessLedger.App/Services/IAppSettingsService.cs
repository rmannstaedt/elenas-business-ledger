using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.App.Services;

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
