using System.Windows;

namespace ElenasBusinessLedger.App.Services;

public sealed class MessageBoxStudentOverviewInteractionService : IStudentOverviewInteractionService
{
    public Task<StudentEditSaveChangesChoice> PromptSaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = MessageBox.Show(
            "Save changes to the selected student?",
            "Save student changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question,
            MessageBoxResult.Cancel);

        var choice = result switch
        {
            MessageBoxResult.Yes => StudentEditSaveChangesChoice.Save,
            MessageBoxResult.No => StudentEditSaveChangesChoice.Discard,
            _ => StudentEditSaveChangesChoice.Cancel
        };

        return Task.FromResult(choice);
    }
}
