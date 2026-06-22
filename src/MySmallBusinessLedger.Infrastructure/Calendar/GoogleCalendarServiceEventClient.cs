using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Lessons;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

namespace MySmallBusinessLedger.Infrastructure.Calendar;

public sealed class GoogleCalendarServiceEventClient : IGoogleCalendarEventClient
{
    private readonly CalendarService calendarService;
    private readonly EventPageLoader? eventPageLoader;
    private readonly string timeZoneId;

    internal delegate Task<Events> EventPageLoader(
        string calendarId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive,
        string? pageToken,
        CancellationToken cancellationToken);

    public GoogleCalendarServiceEventClient(CalendarService calendarService, string timeZoneId)
    {
        this.calendarService = calendarService;
        this.timeZoneId = timeZoneId;
    }

    internal GoogleCalendarServiceEventClient(string timeZoneId, EventPageLoader eventPageLoader)
    {
        this.calendarService = null!;
        this.timeZoneId = timeZoneId;
        this.eventPageLoader = eventPageLoader;
    }

    public async Task<IReadOnlyList<GoogleCalendarEvent>> ListEventsAsync(
        string calendarId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive,
        CancellationToken cancellationToken)
    {
        var events = new List<GoogleCalendarEvent>();
        string? pageToken = null;
        do
        {
            var response = await ListEventsPageAsync(
                calendarId,
                fromInclusive,
                toExclusive,
                pageToken,
                cancellationToken);

            events.AddRange((response.Items ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item.Id) && item.Start?.DateTimeDateTimeOffset is not null)
                .Select(item => new GoogleCalendarEvent(
                    item.Id,
                    item.Summary ?? string.Empty,
                    item.Start!.DateTimeDateTimeOffset!.Value)));

            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrWhiteSpace(pageToken));

        return events;
    }

    private async Task<Events> ListEventsPageAsync(
        string calendarId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive,
        string? pageToken,
        CancellationToken cancellationToken)
    {
        if (eventPageLoader is not null)
        {
            return await eventPageLoader(calendarId, fromInclusive, toExclusive, pageToken, cancellationToken);
        }

        var request = calendarService.Events.List(calendarId);
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        request.TimeMinDateTimeOffset = fromInclusive;
        request.TimeMaxDateTimeOffset = toExclusive;
        request.PageToken = pageToken;

        return await request.ExecuteAsync(cancellationToken);
    }

    public async Task<string> CreateLessonEventAsync(
        string calendarId,
        string summary,
        DateTimeOffset startsAt,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        var calendarEvent = new Event
        {
            Summary = summary,
            Start = CreateEventDateTime(startsAt),
            End = CreateEventDateTime(startsAt.Add(duration))
        };

        var request = calendarService.Events.Insert(calendarEvent, calendarId);
        var created = await request.ExecuteAsync(cancellationToken);
        return created.Id;
    }

    public async Task MoveEventAsync(
        string calendarId,
        string eventId,
        DateTimeOffset startsAt,
        CancellationToken cancellationToken)
    {
        var getRequest = calendarService.Events.Get(calendarId, eventId);
        var existing = await getRequest.ExecuteAsync(cancellationToken);

        var duration = (existing.End?.DateTimeDateTimeOffset - existing.Start?.DateTimeDateTimeOffset)
            ?? TimeSpan.FromHours(1);

        existing.Start = CreateEventDateTime(startsAt);
        existing.End = CreateEventDateTime(startsAt.Add(duration));

        var updateRequest = calendarService.Events.Update(existing, calendarId, eventId);
        await updateRequest.ExecuteAsync(cancellationToken);
    }

    public Task DeleteEventAsync(string calendarId, string eventId, CancellationToken cancellationToken) =>
        calendarService.Events.Delete(calendarId, eventId).ExecuteAsync(cancellationToken);

    public async Task UpdateLessonStateAsync(
        string calendarId,
        string eventId,
        LessonState state,
        CancellationToken cancellationToken)
    {
        var getRequest = calendarService.Events.Get(calendarId, eventId);
        var existing = await getRequest.ExecuteAsync(cancellationToken);
        existing.Summary = CalendarLessonEntry.WithState(existing.Summary ?? string.Empty, state);

        var updateRequest = calendarService.Events.Update(existing, calendarId, eventId);
        await updateRequest.ExecuteAsync(cancellationToken);
    }

    private EventDateTime CreateEventDateTime(DateTimeOffset value) =>
        new()
        {
            DateTimeDateTimeOffset = value,
            TimeZone = timeZoneId
        };
}
