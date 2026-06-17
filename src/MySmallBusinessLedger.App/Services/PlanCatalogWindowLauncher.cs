using System.Windows;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;

namespace MySmallBusinessLedger.App.Services;

public sealed class PlanCatalogWindowLauncher : IPlanCatalogLauncher
{
    private readonly IPlanCatalogService planCatalogService;
    private readonly IPlanCatalogInteractionService interactionService;

    public PlanCatalogWindowLauncher(IPlanCatalogService planCatalogService)
        : this(planCatalogService, new MessageBoxPlanCatalogInteractionService())
    {
    }

    public PlanCatalogWindowLauncher(
        IPlanCatalogService planCatalogService,
        IPlanCatalogInteractionService interactionService)
    {
        this.planCatalogService = planCatalogService;
        this.interactionService = interactionService;
    }

    public async Task ShowPlanCatalogAsync(CancellationToken cancellationToken)
    {
        var viewModel = new PlanCatalogViewModel(planCatalogService, interactionService);
        await viewModel.LoadAsync();
        var window = new PlanCatalogView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        window.ShowDialog();
    }
}
