namespace ElenasBusinessLedger.App.Services;

// Lets the dashboard open the plan editor through a window-independent command dependency.
public interface IPlanCatalogLauncher
{
    Task ShowPlanCatalogAsync(CancellationToken cancellationToken);
}
