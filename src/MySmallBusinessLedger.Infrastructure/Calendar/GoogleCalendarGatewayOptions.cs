namespace MySmallBusinessLedger.Infrastructure.Calendar;

public sealed record GoogleCalendarGatewayOptions(
    string CalendarId,
    TimeZoneInfo TimeZone,
    TimeSpan DefaultLessonDuration);
