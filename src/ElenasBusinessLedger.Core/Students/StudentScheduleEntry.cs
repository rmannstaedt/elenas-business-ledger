namespace ElenasBusinessLedger.Core.Students;

// Holds a recurring weekday and start time so monthly appointments can be generated from a student's schedule.
public sealed record StudentScheduleEntry(DayOfWeek Weekday, TimeOnly StartsAt);
