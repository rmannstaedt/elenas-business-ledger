using MySmallBusinessLedger.Core.Plans;

namespace MySmallBusinessLedger.App.ViewModels;

public sealed class PlanCatalogDisplayModel
{
    public PlanCatalogDisplayModel(PlanCatalogItem plan)
    {
        Plan = plan;
    }

    public PlanCatalogItem Plan { get; }

    public int Id => Plan.Id;

    public string Name => Plan.Name;

    public PlanCatalogState State => Plan.State;

    public string StateText => Plan.State switch
    {
        PlanCatalogState.Draft => "Draft",
        PlanCatalogState.Active => "Active",
        PlanCatalogState.Archived => "Archived",
        _ => "Unknown"
    };

    public string PriceWithoutVatText => Plan.PriceWithoutVat.ToString("0.##");
}
