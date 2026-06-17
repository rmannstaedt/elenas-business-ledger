using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.App.Services;

public sealed record AppSettingsSnapshot(
    CompanyProfile? CompanyProfile,
    CalendarSettings? CalendarSettings,
    GoogleOAuthClientConfigurationStatus GoogleOAuthAppConfigurationStatus,
    string AppDataFolderPath = "",
    string DatabasePath = "",
    string GoogleTokenFolderPath = "");

public interface IAppSettingsService
{
    Task<AppSettingsSnapshot> LoadAsync(CancellationToken cancellationToken);

    Task<OperationResult<AppSettingsSnapshot>> SaveAsync(
        CompanyProfile companyProfile,
        CalendarSettings calendarSettings,
        CancellationToken cancellationToken);

    Task<OperationResult<bool>> ConnectGoogleCalendarAsync(
        CalendarSettings calendarSettings,
        CancellationToken cancellationToken);

    Task<OperationResult<bool>> ForgetGoogleAuthorizationAsync(CancellationToken cancellationToken);
}
