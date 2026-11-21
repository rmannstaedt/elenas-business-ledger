namespace ElenasBusinessLedger.App.Services;

public enum PlanCatalogAddPlanChoice
{
    Save,
    Discard,
    Cancel
}

// Lets the plan editor ask how to handle unsaved edits before a new draft or window close.
public interface IPlanCatalogInteractionService
{
    Task<PlanCatalogAddPlanChoice> PromptAddPlanSaveChangesAsync(CancellationToken cancellationToken);

    Task<PlanCatalogAddPlanChoice> PromptClosePlanSaveChangesAsync(CancellationToken cancellationToken);
}
