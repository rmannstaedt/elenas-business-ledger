namespace ElenasBusinessLedger.App.Services;

// Resolves unsaved-plan prompts as discard when the view model runs without a dialog presenter.
public sealed class DiscardPlanCatalogInteractionService : IPlanCatalogInteractionService
{
    public Task<PlanCatalogAddPlanChoice> PromptAddPlanSaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PlanCatalogAddPlanChoice.Discard);
    }

    public Task<PlanCatalogAddPlanChoice> PromptClosePlanSaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PlanCatalogAddPlanChoice.Discard);
    }
}
