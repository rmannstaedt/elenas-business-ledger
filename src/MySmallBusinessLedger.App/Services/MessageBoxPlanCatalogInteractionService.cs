using System.Windows;

namespace MySmallBusinessLedger.App.Services;

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
}
