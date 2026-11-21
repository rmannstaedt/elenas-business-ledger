namespace ElenasBusinessLedger.App.Services;

public sealed class DiscardStudentOverviewInteractionService : IStudentOverviewInteractionService
{
    public Task<StudentEditSaveChangesChoice> PromptSaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(StudentEditSaveChangesChoice.Discard);
    }
}
