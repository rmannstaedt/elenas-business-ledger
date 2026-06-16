namespace ElenaSmallBusinessLedger.App.ViewModels;

public sealed class ScheduleEntryDisplayModel
{
    public ScheduleEntryDisplayModel(DayOfWeek weekday, TimeOnly startsAt)
    {
        Weekday = weekday;
        StartsAt = startsAt;
    }

    public DayOfWeek Weekday { get; }

    public TimeOnly StartsAt { get; }

    public string DisplayText => $"{Weekday} {StartsAt:HH\\:mm}";
}
