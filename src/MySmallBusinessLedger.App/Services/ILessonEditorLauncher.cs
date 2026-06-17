using MySmallBusinessLedger.App.ViewModels;

namespace MySmallBusinessLedger.App.Services;

public interface ILessonEditorLauncher
{
    Task<bool> ShowAddLessonAsync(string studentShortName, DateTimeOffset startsAt, DateOnly refreshMonth, CancellationToken cancellationToken);

    Task<bool> ShowEditLessonAsync(LessonDisplayModel lesson, DateOnly refreshMonth, CancellationToken cancellationToken);
}
