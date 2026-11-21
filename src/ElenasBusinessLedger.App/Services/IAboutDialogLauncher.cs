namespace ElenasBusinessLedger.App.Services;

public interface IAboutDialogLauncher
{
    Task ShowAboutAsync(CancellationToken cancellationToken);
}
