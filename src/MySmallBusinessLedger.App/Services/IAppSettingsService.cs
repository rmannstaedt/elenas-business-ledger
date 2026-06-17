using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.App.Services;

public sealed record AppSettingsSnapshot(
    CompanyProfile? CompanyProfile,
    CalendarSettings? CalendarSettings,
    bool HasGoogleClientSecrets,
    string AppDataFolderPath = "",
    string DatabasePath = "",
    string GoogleClientSecretsPath = "",
    string GoogleTokenFolderPath = "");

public interface IAppSettingsService
{
    Task<AppSettingsSnapshot> LoadAsync(CancellationToken cancellationToken);

    Task<OperationResult<AppSettingsSnapshot>> SaveAsync(
        CompanyProfile companyProfile,
        CalendarSettings calendarSettings,
        string? googleClientSecretsSourcePath,
        CancellationToken cancellationToken);
}
