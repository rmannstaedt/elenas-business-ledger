namespace MySmallBusinessLedger.App.Services;

public interface IEconomyOverviewLauncher
{
    Task ShowEconomyOverviewAsync(CancellationToken cancellationToken);
}
