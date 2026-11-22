namespace ElenasBusinessLedger.App.ViewModels;

// Debounces draft changes and gives each draft at most one in-flight save on the UI thread.
internal sealed class DraftLessonAutosave(Func<LessonDisplayModel, Task> saveDraftAsync)
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(250);
    private readonly Dictionary<LessonDisplayModel, object> pending = [];
    private readonly HashSet<LessonDisplayModel> saving = [];

    // Schedules a complete draft and supersedes any older request for the same row.
    public void Schedule(LessonDisplayModel lesson)
    {
        if (!lesson.IsDraft || lesson.PlanCatalogId <= 0 || !lesson.TryGetStartsAt(out _))
        {
            return;
        }

        var request = new object();
        pending[lesson] = request;
        _ = SaveAfterDelayAsync(lesson, request);
    }

    // Cancels a queued request when its row is removed.
    public void CancelPending(LessonDisplayModel lesson) => pending.Remove(lesson);

    // Supersedes the delayed request and serializes auto and explicit saves for the same draft.
    public async Task SaveNowAsync(LessonDisplayModel lesson)
    {
        if (!saving.Add(lesson))
        {
            return;
        }

        CancelPending(lesson);
        try
        {
            await saveDraftAsync(lesson);
        }
        finally
        {
            CancelPending(lesson);
            saving.Remove(lesson);
        }
    }

    private async Task SaveAfterDelayAsync(LessonDisplayModel lesson, object request)
    {
        await Task.Delay(Delay);
        if (lesson.IsDraft && pending.TryGetValue(lesson, out var latest) && ReferenceEquals(latest, request))
        {
            await SaveNowAsync(lesson);
        }
    }
}
