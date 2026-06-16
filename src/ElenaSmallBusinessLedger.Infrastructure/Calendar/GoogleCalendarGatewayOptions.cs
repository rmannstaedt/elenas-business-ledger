namespace ElenaSmallBusinessLedger.Infrastructure.Calendar;

public sealed record GoogleCalendarGatewayOptions(
    string CalendarId,
    TimeZoneInfo TimeZone,
    TimeSpan DefaultLessonDuration);
