namespace MySmallBusinessLedger.App.Services;

public sealed class DiscardPlanCatalogInteractionService : IPlanCatalogInteractionService
{
    public Task<PlanCatalogAddPlanChoice> PromptAddPlanSaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PlanCatalogAddPlanChoice.Discard);
    }
}
