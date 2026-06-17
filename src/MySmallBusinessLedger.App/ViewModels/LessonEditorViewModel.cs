using System.Globalization;
using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.UseCases.Lessons;

namespace MySmallBusinessLedger.App.ViewModels;

public sealed class LessonEditorViewModel : ViewModelBase
{
    private const string DateFormat = "dd-MM-yyyy";
    private readonly ILessonEditorService lessonEditorService;
    private readonly string? lessonId;
    private readonly bool isNewLesson;
    private readonly DateOnly refreshMonth;
    private string studentShortName;
    private string dateText;
    private string timeText;
    private string? errorMessage;
    private bool isBusy;

    private LessonEditorViewModel(
        ILessonEditorService lessonEditorService,
        string? lessonId,
        string studentShortName,
        DateTimeOffset startsAt,
        bool isInvoiced,
        DateOnly refreshMonth,
        bool isNewLesson)
    {
        this.lessonEditorService = lessonEditorService;
        this.lessonId = lessonId;
        this.studentShortName = studentShortName;
        this.refreshMonth = refreshMonth;
        this.isNewLesson = isNewLesson;
        IsInvoiced = isInvoiced;
        dateText = startsAt.ToString(DateFormat, CultureInfo.InvariantCulture);
        timeText = startsAt.ToString("HH:mm", CultureInfo.InvariantCulture);

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => !IsBusy && !IsInvoiced && !isNewLesson);
    }

    public event EventHandler<bool?>? CloseRequested;

    public static LessonEditorViewModel CreateNew(
        ILessonEditorService lessonEditorService,
        string studentShortName,
        DateTimeOffset startsAt,
        DateOnly refreshMonth) =>
        new(lessonEditorService, null, studentShortName, startsAt, isInvoiced: false, refreshMonth, isNewLesson: true);

    public static LessonEditorViewModel EditExisting(
        ILessonEditorService lessonEditorService,
        string lessonId,
        string studentShortName,
        DateTimeOffset startsAt,
        bool isInvoiced,
        DateOnly refreshMonth) =>
        new(lessonEditorService, lessonId, studentShortName, startsAt, isInvoiced, refreshMonth, isNewLesson: false);

    public string StudentShortName
    {
        get => studentShortName;
        set => SetProperty(ref studentShortName, value);
    }

    public string DateText
    {
        get => dateText;
        set => SetProperty(ref dateText, value);
    }

    public string TimeText
    {
        get => timeText;
        set => SetProperty(ref timeText, value);
    }

    public bool IsInvoiced { get; }

    public string? ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool WasSaved { get; private set; }

    public bool WasDeleted { get; private set; }

    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand DeleteCommand { get; }

    private async Task SaveAsync()
    {
        if (IsInvoiced)
        {
            ErrorMessage = "Invoiced lessons cannot be edited.";
            return;
        }

        if (!TryCreateStartsAt(out var startsAt))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = isNewLesson
                ? await lessonEditorService.AddAsync(new LessonAddRequest(StudentShortName, startsAt), refreshMonth, CancellationToken.None)
                : await lessonEditorService.EditAsync(new LessonEditRequest(lessonId!, StudentShortName, startsAt), refreshMonth, CancellationToken.None);

            if (!result.Succeeded)
            {
                ShowFailure(result, "Google Calendar lesson update failed.");
                return;
            }

            WasSaved = true;
            ErrorMessage = null;
            CloseRequested?.Invoke(this, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (IsInvoiced)
        {
            ErrorMessage = "Invoiced lessons cannot be cancelled.";
            return;
        }

        if (lessonId is null)
        {
            ErrorMessage = "New lessons can be cancelled by closing the editor.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await lessonEditorService.DeleteAsync(lessonId, refreshMonth, CancellationToken.None);
            if (!result.Succeeded)
            {
                ShowFailure(result, "Google Calendar lesson delete failed.");
                return;
            }

            WasDeleted = true;
            ErrorMessage = null;
            CloseRequested?.Invoke(this, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool TryCreateStartsAt(out DateTimeOffset startsAt)
    {
        startsAt = default;
        if (!DateOnly.TryParseExact(DateText, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            ErrorMessage = "Lesson date must use dd-MM-yyyy.";
            return false;
        }

        if (!TimeOnly.TryParseExact(TimeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            ErrorMessage = "Lesson time must use HH:mm.";
            return false;
        }

        startsAt = new DateTimeOffset(date.ToDateTime(time), TimeSpan.Zero);
        ErrorMessage = null;
        return true;
    }

    private void ShowFailure<T>(OperationResult<T> result, string fallbackMessage)
    {
        ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? fallbackMessage;
    }
}
