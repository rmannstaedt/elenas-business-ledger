namespace MySmallBusinessLedger.App.Services;

public interface IEconomyOverviewLauncher
{
    Task ShowEconomyOverviewAsync(bool isAdminMode, CancellationToken cancellationToken);
}
