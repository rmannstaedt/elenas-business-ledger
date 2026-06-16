using System.IO;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Company;
using ElenaSmallBusinessLedger.Core.Ports;
using ElenaSmallBusinessLedger.Infrastructure.Persistence;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class CoreAppSettingsService : IAppSettingsService
{
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly SqliteCalendarSettingsRepository calendarSettingsRepository;
    private readonly string googleClientSecretsPath;

    public CoreAppSettingsService(
        ICompanyProfileRepository companyProfileRepository,
        SqliteCalendarSettingsRepository calendarSettingsRepository,
        string appDataPath)
    {
        this.companyProfileRepository = companyProfileRepository;
        this.calendarSettingsRepository = calendarSettingsRepository;
        googleClientSecretsPath = Path.Combine(appDataPath, "google-client-secrets.json");
    }

    public async Task<AppSettingsSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        CompanyProfile? companyProfile = null;
        try
        {
            companyProfile = await companyProfileRepository.GetAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            companyProfile = null;
        }

        var calendarSettings = await calendarSettingsRepository.GetAsync(cancellationToken);
        return new AppSettingsSnapshot(companyProfile, calendarSettings, File.Exists(googleClientSecretsPath));
    }

    public async Task<OperationResult<AppSettingsSnapshot>> SaveAsync(
        CompanyProfile companyProfile,
        CalendarSettings calendarSettings,
        string? googleClientSecretsSourcePath,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(googleClientSecretsSourcePath))
            {
                var sourcePath = Path.GetFullPath(googleClientSecretsSourcePath);
                if (!File.Exists(sourcePath))
                {
                    return OperationResult<AppSettingsSnapshot>.Failure(
                        new ValidationError("Settings.GoogleClientSecretsMissing", "The Google client secrets file was not found."));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(googleClientSecretsPath)!);
                File.Copy(sourcePath, googleClientSecretsPath, overwrite: true);
            }

            await companyProfileRepository.SaveAsync(companyProfile, cancellationToken);
            await calendarSettingsRepository.SaveAsync(calendarSettings, cancellationToken);
            return OperationResult<AppSettingsSnapshot>.Success(
                new AppSettingsSnapshot(
                    companyProfile,
                    calendarSettings,
                    File.Exists(googleClientSecretsPath)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<AppSettingsSnapshot>.Failure(
                new ValidationError("Settings.SaveFailed", ex.Message));
        }
    }
}
