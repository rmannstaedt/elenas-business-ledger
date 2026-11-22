namespace ElenasBusinessLedger.App.ViewModels;

// Formats one recurring lesson time for display in the student schedule.
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
