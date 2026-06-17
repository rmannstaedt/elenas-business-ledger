using System.Windows;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;

namespace MySmallBusinessLedger.App.Services;

public sealed class PlanCatalogWindowLauncher : IPlanCatalogLauncher
{
    private readonly IPlanCatalogService planCatalogService;

    public PlanCatalogWindowLauncher(IPlanCatalogService planCatalogService)
    {
        this.planCatalogService = planCatalogService;
    }

    public async Task ShowPlanCatalogAsync(CancellationToken cancellationToken)
    {
        var viewModel = new PlanCatalogViewModel(planCatalogService);
        await viewModel.LoadAsync();
        var window = new PlanCatalogView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        window.ShowDialog();
    }
}
