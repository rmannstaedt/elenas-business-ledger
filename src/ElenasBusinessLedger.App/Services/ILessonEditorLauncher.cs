using ElenasBusinessLedger.App.ViewModels;

namespace ElenasBusinessLedger.App.Services;

// Opens standalone add or edit dialogs when a caller needs a separate lesson editor.
public interface ILessonEditorLauncher
{
    Task<bool> ShowAddLessonAsync(string studentShortName, DateTimeOffset startsAt, DateOnly refreshMonth, CancellationToken cancellationToken);

    Task<bool> ShowEditLessonAsync(LessonDisplayModel lesson, DateOnly refreshMonth, CancellationToken cancellationToken);
}
