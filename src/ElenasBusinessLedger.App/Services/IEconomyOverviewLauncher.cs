namespace ElenasBusinessLedger.App.Services;

public interface IEconomyOverviewLauncher
{
    Task ShowEconomyOverviewAsync(bool isAdminMode, CancellationToken cancellationToken);
}
