namespace MySmallBusinessLedger.App.Services;

public interface ISettingsLauncher
{
    Task<bool> ShowSettingsAsync(CancellationToken cancellationToken);
}
