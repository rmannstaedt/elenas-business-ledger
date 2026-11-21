namespace ElenasBusinessLedger.App.Services;

public interface IPlanCatalogLauncher
{
    Task ShowPlanCatalogAsync(CancellationToken cancellationToken);
}
