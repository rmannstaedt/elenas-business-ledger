namespace ElenasBusinessLedger.App.Services;

public enum StudentEditSaveChangesChoice
{
    Save,
    Discard,
    Cancel
}

public interface IStudentOverviewInteractionService
{
    Task<StudentEditSaveChangesChoice> PromptSaveChangesAsync(CancellationToken cancellationToken);
}
