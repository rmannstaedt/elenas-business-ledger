namespace MySmallBusinessLedger.App.ViewModels;

public sealed record WeekdayOption(string Label, DayOfWeek? Weekday)
{
    public static WeekdayOption Unspecified { get; } = new("Unspecified", null);
}

public sealed class SchedulePlacementViewModel : ViewModelBase
{
    private WeekdayOption selectedWeekday;
    private string timeText = string.Empty;

    public SchedulePlacementViewModel(string label, IReadOnlyList<WeekdayOption> weekdayOptions)
    {
        Label = label;
        WeekdayOptions = weekdayOptions;
        selectedWeekday = WeekdayOption.Unspecified;
    }

    public string Label { get; }

    public IReadOnlyList<WeekdayOption> WeekdayOptions { get; }

    public WeekdayOption SelectedWeekday
    {
        get => selectedWeekday;
        set => SetProperty(ref selectedWeekday, value);
    }

    public string TimeText
    {
        get => timeText;
        set => SetProperty(ref timeText, value);
    }
}
