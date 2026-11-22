namespace ElenasBusinessLedger.App.Services;

public enum StudentEditSaveChangesChoice
{
    Save,
    Discard,
    Cancel
}

// Lets the shared student editor ask about unsaved changes before navigation or closing.
public interface IStudentOverviewInteractionService
{
    Task<StudentEditSaveChangesChoice> PromptSaveChangesAsync(CancellationToken cancellationToken);
}
