using System.IO;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.App.Services;

public sealed class CoreAppSettingsService : IAppSettingsService
{
    private readonly ICompanyProfileRepository companyProfileRepository;
    private readonly SqliteCalendarSettingsRepository calendarSettingsRepository;
    private readonly AppPaths appPaths;

    public CoreAppSettingsService(
        ICompanyProfileRepository companyProfileRepository,
        SqliteCalendarSettingsRepository calendarSettingsRepository,
        string appDataPath)
        : this(companyProfileRepository, calendarSettingsRepository, AppPaths.FromAppDataFolder(appDataPath))
    {
    }

    public CoreAppSettingsService(
        ICompanyProfileRepository companyProfileRepository,
        SqliteCalendarSettingsRepository calendarSettingsRepository,
        AppPaths appPaths)
    {
        this.companyProfileRepository = companyProfileRepository;
        this.calendarSettingsRepository = calendarSettingsRepository;
        this.appPaths = appPaths;
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
        return CreateSnapshot(companyProfile, calendarSettings);
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
                        new ValidationError("Settings.GoogleClientSecretsMissing", "The Google Calendar login setup file was not found."));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(appPaths.GoogleClientSecretsPath)!);
                File.Copy(sourcePath, appPaths.GoogleClientSecretsPath, overwrite: true);
            }

            await companyProfileRepository.SaveAsync(companyProfile, cancellationToken);
            await calendarSettingsRepository.SaveAsync(calendarSettings, cancellationToken);
            return OperationResult<AppSettingsSnapshot>.Success(
                CreateSnapshot(companyProfile, calendarSettings));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<AppSettingsSnapshot>.Failure(
                new ValidationError("Settings.SaveFailed", ex.Message));
        }
    }

    private AppSettingsSnapshot CreateSnapshot(
        CompanyProfile? companyProfile,
        CalendarSettings? calendarSettings) =>
        new(
            companyProfile,
            calendarSettings,
            File.Exists(appPaths.GoogleClientSecretsPath),
            appPaths.AppDataFolderPath,
            appPaths.DatabasePath,
            appPaths.GoogleClientSecretsPath,
            appPaths.GoogleTokenFolderPath);
}
