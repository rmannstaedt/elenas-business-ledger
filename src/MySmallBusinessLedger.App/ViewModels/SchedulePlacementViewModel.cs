namespace MySmallBusinessLedger.App.ViewModels;

public sealed record WeekdayOption(string Label, DayOfWeek? Weekday)
{
    public static WeekdayOption Unspecified { get; } = new("Unspecified", null);
}

public sealed class SchedulePlacementViewModel : ViewModelBase
{
    private static readonly IReadOnlyList<string> DefaultTimeOptions = CreateTimeOptions();
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

    public IReadOnlyList<string> TimeOptions => DefaultTimeOptions;

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

    private static IReadOnlyList<string> CreateTimeOptions()
    {
        var options = new List<string>();
        for (var minutes = 8 * 60; minutes <= 21 * 60 + 30; minutes += 30)
        {
            options.Add(new TimeOnly(minutes / 60, minutes % 60).ToString("HH\\:mm"));
        }

        return options;
    }
}
