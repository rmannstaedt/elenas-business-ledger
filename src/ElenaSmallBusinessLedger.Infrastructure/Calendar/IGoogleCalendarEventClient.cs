namespace ElenaSmallBusinessLedger.Infrastructure.Calendar;

public interface IGoogleCalendarEventClient
{
    Task<IReadOnlyList<GoogleCalendarEvent>> ListEventsAsync(
        string calendarId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive,
        CancellationToken cancellationToken);

    Task<string> CreateLessonEventAsync(
        string calendarId,
        string summary,
        DateTimeOffset startsAt,
        TimeSpan duration,
        CancellationToken cancellationToken);

    Task MoveEventAsync(
        string calendarId,
        string eventId,
        DateTimeOffset startsAt,
        CancellationToken cancellationToken);

    Task DeleteEventAsync(
        string calendarId,
        string eventId,
        CancellationToken cancellationToken);
}
