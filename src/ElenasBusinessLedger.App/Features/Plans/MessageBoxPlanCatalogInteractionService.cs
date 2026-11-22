using System.Windows;

namespace ElenasBusinessLedger.App.Services;

// Asks whether to save, discard, or keep editing when plan navigation would leave changed fields behind.
public sealed class MessageBoxPlanCatalogInteractionService : IPlanCatalogInteractionService
{
    public Task<PlanCatalogAddPlanChoice> PromptAddPlanSaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = MessageBox.Show(
            "Save changes to the selected plan before adding a new plan?",
            "Save plan changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question,
            MessageBoxResult.Cancel);

        var choice = result switch
        {
            MessageBoxResult.Yes => PlanCatalogAddPlanChoice.Save,
            MessageBoxResult.No => PlanCatalogAddPlanChoice.Discard,
            _ => PlanCatalogAddPlanChoice.Cancel
        };

        return Task.FromResult(choice);
    }

    public Task<PlanCatalogAddPlanChoice> PromptClosePlanSaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = MessageBox.Show(
            "Save changes to the selected plan before closing Manage Plans?",
            "Save plan changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question,
            MessageBoxResult.Cancel);

        var choice = result switch
        {
            MessageBoxResult.Yes => PlanCatalogAddPlanChoice.Save,
            MessageBoxResult.No => PlanCatalogAddPlanChoice.Discard,
            _ => PlanCatalogAddPlanChoice.Cancel
        };

        return Task.FromResult(choice);
    }
}
