namespace ElenasBusinessLedger.App.Services;

// Resolves unsaved-student prompts as discard when no interactive prompt is configured.
public sealed class DiscardStudentOverviewInteractionService : IStudentOverviewInteractionService
{
    public Task<StudentEditSaveChangesChoice> PromptSaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(StudentEditSaveChangesChoice.Discard);
    }
}
