namespace ElenasBusinessLedger.Infrastructure.Calendar;

public sealed record GoogleCalendarGatewayOptions(
    string CalendarId,
    TimeZoneInfo TimeZone,
    TimeSpan DefaultLessonDuration);
