namespace ElenaSmallBusinessLedger.Infrastructure.Calendar;

public sealed record GoogleCalendarEvent(string Id, string Summary, DateTimeOffset StartsAt);
