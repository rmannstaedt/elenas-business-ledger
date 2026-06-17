namespace MySmallBusinessLedger.App.Services;

public enum PlanCatalogAddPlanChoice
{
    Save,
    Discard,
    Cancel
}

public interface IPlanCatalogInteractionService
{
    Task<PlanCatalogAddPlanChoice> PromptAddPlanSaveChangesAsync(CancellationToken cancellationToken);
}
