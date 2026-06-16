using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Company;
using ElenaSmallBusinessLedger.Infrastructure.Persistence;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed record AppSettingsSnapshot(
    CompanyProfile? CompanyProfile,
    CalendarSettings? CalendarSettings,
    bool HasGoogleClientSecrets);

public interface IAppSettingsService
{
    Task<AppSettingsSnapshot> LoadAsync(CancellationToken cancellationToken);

    Task<OperationResult<AppSettingsSnapshot>> SaveAsync(
        CompanyProfile companyProfile,
        CalendarSettings calendarSettings,
        string? googleClientSecretsSourcePath,
        CancellationToken cancellationToken);
}
