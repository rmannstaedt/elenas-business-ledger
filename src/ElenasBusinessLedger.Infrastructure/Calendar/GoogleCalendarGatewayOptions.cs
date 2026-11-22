namespace ElenasBusinessLedger.Infrastructure.Calendar;

// Supplies the calendar, time zone, duration, and scope needed to interpret events and produce scoped IDs.
public sealed record GoogleCalendarGatewayOptions(
    string CalendarId,
    TimeZoneInfo TimeZone,
    TimeSpan DefaultLessonDuration,
    string? CalendarScope = null);
