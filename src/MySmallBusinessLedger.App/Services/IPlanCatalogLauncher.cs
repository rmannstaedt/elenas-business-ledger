namespace MySmallBusinessLedger.App.Services;

public interface IPlanCatalogLauncher
{
    Task ShowPlanCatalogAsync(CancellationToken cancellationToken);
}
