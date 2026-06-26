using MySmallBusinessLedger.Core.Lessons;

namespace MySmallBusinessLedger.Infrastructure.Calendar;

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
        bool hasKnownStartTime,
        TimeSpan duration,
        CancellationToken cancellationToken);

    Task MoveEventAsync(
        string calendarId,
        string eventId,
        DateTimeOffset startsAt,
        bool hasKnownStartTime,
        CancellationToken cancellationToken);

    Task DeleteEventAsync(
        string calendarId,
        string eventId,
        CancellationToken cancellationToken);

    Task UpdateLessonStateAsync(
        string calendarId,
        string eventId,
        LessonState state,
        CancellationToken cancellationToken);
}
