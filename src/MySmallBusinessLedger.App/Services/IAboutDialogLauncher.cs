namespace MySmallBusinessLedger.App.Services;

public interface IAboutDialogLauncher
{
    Task ShowAboutAsync(CancellationToken cancellationToken);
}
