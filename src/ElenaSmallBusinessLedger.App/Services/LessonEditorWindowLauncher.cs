using System.Windows;
using ElenaSmallBusinessLedger.App.ViewModels;
using ElenaSmallBusinessLedger.App.Views;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class LessonEditorWindowLauncher : ILessonEditorLauncher
{
    private readonly ILessonEditorService lessonEditorService;

    public LessonEditorWindowLauncher(ILessonEditorService lessonEditorService)
    {
        this.lessonEditorService = lessonEditorService;
    }

    public Task<bool> ShowAddLessonAsync(
        string studentShortName,
        DateTimeOffset startsAt,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        var viewModel = LessonEditorViewModel.CreateNew(lessonEditorService, studentShortName, startsAt, refreshMonth);
        return Task.FromResult(ShowDialog(viewModel));
    }

    public Task<bool> ShowEditLessonAsync(
        LessonDisplayModel lesson,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        var viewModel = LessonEditorViewModel.EditExisting(
            lessonEditorService,
            lesson.Id,
            lesson.StudentShortName,
            lesson.StartsAt,
            lesson.IsInvoiced,
            refreshMonth);
        return Task.FromResult(ShowDialog(viewModel));
    }

    private static bool ShowDialog(LessonEditorViewModel viewModel)
    {
        var window = new LessonEditorView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        return window.ShowDialog() == true && (viewModel.WasSaved || viewModel.WasDeleted);
    }
}
