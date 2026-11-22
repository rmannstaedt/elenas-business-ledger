using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Lessons;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using System.Globalization;

namespace ElenasBusinessLedger.Infrastructure.Calendar;

// Adapts Google event requests and paging to the operations used by the lesson gateway.
public sealed class GoogleCalendarServiceEventClient : IGoogleCalendarEventClient
{
    private readonly CalendarService calendarService;
    private readonly EventPageLoader? eventPageLoader;
    private readonly EventInsert? eventInsert;
    private readonly string timeZoneId;

    internal delegate Task<Events> EventPageLoader(
        string calendarId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive,
        string? pageToken,
        CancellationToken cancellationToken);

    internal delegate Task<Event> EventInsert(
        string calendarId,
        Event calendarEvent,
        CancellationToken cancellationToken);

    public GoogleCalendarServiceEventClient(CalendarService calendarService, string timeZoneId)
    {
        this.calendarService = calendarService;
        this.timeZoneId = ToGoogleCalendarTimeZoneId(
            timeZoneId,
            RegionInfo.CurrentRegion.TwoLetterISORegionName);
    }

    internal GoogleCalendarServiceEventClient(string timeZoneId, EventPageLoader eventPageLoader)
    {
        this.calendarService = null!;
        this.timeZoneId = ToGoogleCalendarTimeZoneId(timeZoneId);
        this.eventPageLoader = eventPageLoader;
    }

    internal GoogleCalendarServiceEventClient(string timeZoneId, string? region, EventInsert eventInsert)
    {
        this.calendarService = null!;
        this.timeZoneId = ToGoogleCalendarTimeZoneId(timeZoneId, region);
        this.eventInsert = eventInsert;
    }

    // Collects all event pages so a calendar refresh also includes appointments beyond Google's first page.
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
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .Select(TryCreateGoogleCalendarEvent)
                .Where(calendarEvent => calendarEvent is not null)
                .Select(calendarEvent => calendarEvent!));

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

    // Writes a timed or all-day appointment and returns the external ID used for local linking.
    public async Task<string> CreateLessonEventAsync(
        string calendarId,
        string summary,
        DateTimeOffset startsAt,
        bool hasKnownStartTime,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        var calendarEvent = new Event
        {
            Summary = summary,
            Start = CreateEventDateTime(startsAt, hasKnownStartTime),
            End = CreateEventDateTime(
                hasKnownStartTime ? startsAt.Add(duration) : startsAt.AddDays(1),
                hasKnownStartTime)
        };

        if (eventInsert is not null)
        {
            var inserted = await eventInsert(calendarId, calendarEvent, cancellationToken);
            return inserted.Id;
        }

        var request = calendarService.Events.Insert(calendarEvent, calendarId);
        var created = await request.ExecuteAsync(cancellationToken);
        return created.Id;
    }

    // Moves the existing event, retaining its timed duration where known.
    public async Task MoveEventAsync(
        string calendarId,
        string eventId,
        DateTimeOffset startsAt,
        bool hasKnownStartTime,
        CancellationToken cancellationToken)
    {
        var getRequest = calendarService.Events.Get(calendarId, eventId);
        var existing = await getRequest.ExecuteAsync(cancellationToken);

        var duration = (existing.End?.DateTimeDateTimeOffset - existing.Start?.DateTimeDateTimeOffset)
            ?? TimeSpan.FromHours(1);

        existing.Start = CreateEventDateTime(startsAt, hasKnownStartTime);
        existing.End = CreateEventDateTime(
            hasKnownStartTime ? startsAt.Add(duration) : startsAt.AddDays(1),
            hasKnownStartTime);

        var updateRequest = calendarService.Events.Update(existing, calendarId, eventId);
        await updateRequest.ExecuteAsync(cancellationToken);
    }

    public Task DeleteEventAsync(string calendarId, string eventId, CancellationToken cancellationToken) =>
        calendarService.Events.Delete(calendarId, eventId).ExecuteAsync(cancellationToken);

    // Updates the completion marker on the existing event summary so later refreshes see the new state.
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

    private static GoogleCalendarEvent? TryCreateGoogleCalendarEvent(Event item)
    {
        if (item.Start?.DateTimeDateTimeOffset is { } dateTime)
        {
            return new GoogleCalendarEvent(item.Id, item.Summary ?? string.Empty, dateTime);
        }

        if (!string.IsNullOrWhiteSpace(item.Start?.Date) &&
            DateOnly.TryParseExact(item.Start.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return new GoogleCalendarEvent(
                item.Id,
                item.Summary ?? string.Empty,
                new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                HasKnownStartTime: false);
        }

        return null;
    }

    private EventDateTime CreateEventDateTime(DateTimeOffset value, bool hasKnownStartTime) =>
        hasKnownStartTime
            ? new EventDateTime
            {
                DateTimeDateTimeOffset = value,
                TimeZone = timeZoneId
            }
            : new EventDateTime
            {
                Date = DateOnly.FromDateTime(value.DateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };

    private static string ToGoogleCalendarTimeZoneId(string value, string? region = null)
    {
        if (!string.IsNullOrWhiteSpace(region) &&
            TimeZoneInfo.TryConvertWindowsIdToIanaId(value, region, out var regionalTimeZoneId))
        {
            return regionalTimeZoneId;
        }

        return TimeZoneInfo.TryConvertWindowsIdToIanaId(value, out var timeZoneId)
            ? timeZoneId
            : value;
    }
}
