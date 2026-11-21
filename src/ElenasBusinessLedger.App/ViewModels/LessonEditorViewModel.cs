using System.Globalization;
using ElenasBusinessLedger.App.Commands;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.UseCases.Lessons;

namespace ElenasBusinessLedger.App.ViewModels;

public sealed class LessonEditorViewModel : ViewModelBase
{
    private const string DateFormat = "dd-MM-yyyy";
    private readonly ILessonEditorService lessonEditorService;
    private readonly string? lessonId;
    private readonly bool isNewLesson;
    private readonly DateOnly refreshMonth;
    private readonly TimeZoneInfo lessonTimeZone;
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
        bool isNewLesson,
        TimeZoneInfo? lessonTimeZone)
    {
        this.lessonEditorService = lessonEditorService;
        this.lessonId = lessonId;
        this.studentShortName = studentShortName;
        this.refreshMonth = refreshMonth;
        this.isNewLesson = isNewLesson;
        this.lessonTimeZone = lessonTimeZone ?? TimeZoneInfo.Local;
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
        DateOnly refreshMonth,
        TimeZoneInfo? lessonTimeZone = null) =>
        new(lessonEditorService, null, studentShortName, startsAt, isInvoiced: false, refreshMonth, isNewLesson: true, lessonTimeZone);

    public static LessonEditorViewModel EditExisting(
        ILessonEditorService lessonEditorService,
        string lessonId,
        string studentShortName,
        DateTimeOffset startsAt,
        bool isInvoiced,
        DateOnly refreshMonth,
        TimeZoneInfo? lessonTimeZone = null) =>
        new(lessonEditorService, lessonId, studentShortName, startsAt, isInvoiced, refreshMonth, isNewLesson: false, lessonTimeZone);

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
            const string failureMessage = "Google Calendar lesson update failed.";
            var failed = isNewLesson
                ? ShowFailureIfNeeded(
                    await lessonEditorService.AddAsync(new LessonAddRequest(StudentShortName, startsAt, PlanCatalogId: 0), refreshMonth, CancellationToken.None),
                    failureMessage)
                : ShowFailureIfNeeded(
                    await lessonEditorService.EditAsync(new LessonEditRequest(lessonId!, StudentShortName, startsAt), refreshMonth, CancellationToken.None),
                    failureMessage);

            if (failed)
            {
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
            if (ShowFailureIfNeeded(result, "Google Calendar lesson delete failed."))
            {
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

        var localDateTime = date.ToDateTime(time);
        startsAt = new DateTimeOffset(localDateTime, lessonTimeZone.GetUtcOffset(localDateTime));
        ErrorMessage = null;
        return true;
    }

    private bool ShowFailureIfNeeded<T>(OperationResult<T> result, string fallbackMessage)
    {
        if (result.Succeeded)
        {
            return false;
        }

        ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? fallbackMessage;
        return true;
    }
}
