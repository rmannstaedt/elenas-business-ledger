namespace ElenasBusinessLedger.App.Services;

public sealed record SettingsDialogResult(bool WasSaved, bool IsAdminMode);

public interface ISettingsLauncher
{
    Task<SettingsDialogResult> ShowSettingsAsync(bool isAdminMode, CancellationToken cancellationToken);
}
