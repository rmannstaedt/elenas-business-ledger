using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.UseCases.Lessons;

namespace ElenasBusinessLedger.Infrastructure.Calendar;

public sealed class GoogleCalendarGateway : IGoogleCalendarGateway
{
    private readonly IGoogleCalendarEventClient eventClient;
    private readonly GoogleCalendarGatewayOptions options;

    public GoogleCalendarGateway(IGoogleCalendarEventClient eventClient, GoogleCalendarGatewayOptions options)
    {
        this.eventClient = eventClient;
        this.options = options;
    }

    public async Task<IReadOnlyList<CalendarLesson>> ListLessonsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var events = await eventClient.ListEventsAsync(
            options.CalendarId,
            StartOfDay(from),
            StartOfDay(to.AddDays(1)),
            cancellationToken);

        return events
            .Where(calendarEvent => CalendarLessonEntry.TryParse(
                calendarEvent.Summary,
                calendarEvent.StartsAt,
                out _,
                calendarEvent.HasKnownStartTime))
            .Select(calendarEvent =>
            {
                CalendarLessonEntry.TryParse(
                    calendarEvent.Summary,
                    calendarEvent.StartsAt,
                    out var entry,
                    calendarEvent.HasKnownStartTime);
                return CreateCalendarLesson(
                    calendarEvent.Id,
                    entry!.StudentShortName,
                    entry.StartsAt,
                    entry.State,
                    entry.HasKnownStartTime);
            })
            .OrderBy(lesson => lesson.StartsAt)
            .ToArray();
    }

    public async Task<OperationResult<LessonAddResult>> AddLessonAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        string createdLessonId;
        try
        {
            createdLessonId = await eventClient.CreateLessonEventAsync(
                options.CalendarId,
                $"$ {request.StudentShortName}",
                request.StartsAt,
                request.HasKnownStartTime,
                options.DefaultLessonDuration,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<LessonAddResult>.Failure(
                new ValidationError(
                    "Calendar.UpdateFailed",
                    "Google Calendar could not be updated. Refresh Calendar and try again."));
        }

        try
        {
            var lessons = await ListRefreshMonthAsync(refreshMonth, cancellationToken);
            var createdLesson = lessons.FirstOrDefault(lesson =>
                StringComparer.Ordinal.Equals(lesson.Id, CreateScopedId(createdLessonId))) ??
                CreateCalendarLesson(
                    createdLessonId,
                    request.StudentShortName,
                    request.StartsAt,
                    LessonState.Planned,
                    request.HasKnownStartTime);

            return OperationResult<LessonAddResult>.Success(new LessonAddResult(createdLesson, lessons));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<LessonAddResult>.Failure(
                new LessonAddResult(
                    CreateCalendarLesson(
                        createdLessonId,
                        request.StudentShortName,
                        request.StartsAt,
                        LessonState.Planned,
                        request.HasKnownStartTime),
                    []),
                new ValidationError(
                    "Calendar.RefreshFailed",
                    "Google Calendar was updated, but the lesson list could not be refreshed."));
        }
    }

    public Task<OperationResult<IReadOnlyList<CalendarLesson>>> PopulateLessonsAsync(
        IReadOnlyList<LessonAddRequest> requests, DateOnly refreshMonth, CancellationToken cancellationToken) =>
        PopulateWithReceiptsAsync(requests, refreshMonth, (_, _, _) => Task.FromResult<ValidationError?>(null), cancellationToken);

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> PopulateWithReceiptsAsync(
        IReadOnlyList<LessonAddRequest> requests, DateOnly refreshMonth,
        Func<int, CalendarLesson, CancellationToken, Task<ValidationError?>> saveReceipt,
        CancellationToken cancellationToken)
    {
        var createdLessons = new List<CalendarLesson>();
        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            string createdLessonId;
            try
            {
                createdLessonId = await eventClient.CreateLessonEventAsync(
                    options.CalendarId,
                    $"$ {request.StudentShortName}",
                    request.StartsAt,
                    request.HasKnownStartTime,
                    options.DefaultLessonDuration,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(createdLessons.ToArray(), new ValidationError(
                    "Calendar.UpdateFailed", "Google Calendar did not confirm the remaining appointments. The acknowledged appointments have been saved."));
            }

            var created = CreateCalendarLesson(createdLessonId, request.StudentShortName, request.StartsAt,
                LessonState.Planned, request.HasKnownStartTime);
            var error = await saveReceipt(index, created, cancellationToken);
            if (error is not null)
            {
                return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(createdLessons.ToArray(), error);
            }

            createdLessons.Add(created);
        }

        return await RefreshAfterSuccessfulPopulateAsync(refreshMonth, createdLessons, cancellationToken);
    }

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> UpdateLessonAsync(
        LessonEditRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        var receipt = await UpdateWithoutRefreshAsync(request, refreshMonth, cancellationToken);
        return receipt.State == CalendarWriteState.Acknowledged
            ? await RefreshAfterSuccessfulWriteAsync(refreshMonth, cancellationToken)
            : OperationResult<IReadOnlyList<CalendarLesson>>.Failure(receipt.Errors.ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteLessonAsync(
        string lessonId,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        var receipt = await DeleteWithoutRefreshAsync(lessonId, refreshMonth, cancellationToken);
        return receipt.State == CalendarWriteState.Acknowledged
            ? await RefreshAfterSuccessfulWriteAsync(refreshMonth, cancellationToken)
            : OperationResult<IReadOnlyList<CalendarLesson>>.Failure(receipt.Errors.ToArray());
    }

    public Task<CalendarWriteReceipt> UpdateWithoutRefreshAsync(
        LessonEditRequest request, DateOnly refreshMonth, CancellationToken cancellationToken) =>
        WriteEventAsync(request.LessonId, eventId => eventClient.MoveEventAsync(options.CalendarId, eventId,
            request.StartsAt, request.HasKnownStartTime, cancellationToken));

    public Task<CalendarWriteReceipt> DeleteWithoutRefreshAsync(
        string lessonId, DateOnly refreshMonth, CancellationToken cancellationToken) =>
        WriteEventAsync(lessonId, eventId => eventClient.DeleteEventAsync(options.CalendarId, eventId, cancellationToken));

    private async Task<CalendarWriteReceipt> WriteEventAsync(string lessonId, Func<string, Task> write)
    {
        if (GetExternalId(lessonId) is not { } eventId)
        {
            return new(CalendarWriteState.Rejected, WrongCalendar<bool>().Errors);
        }

        try
        {
            await write(eventId);
            return new(CalendarWriteState.Acknowledged, []);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new(CalendarWriteState.Uncertain, [new ValidationError(
                "Calendar.UpdateFailed", "Google Calendar did not confirm the change. Check the calendar before invoicing or retrying.")]);
        }
    }

    public async Task<OperationResult<LessonState>> UpdateLessonStateAsync(
        string lessonId,
        LessonState state,
        CancellationToken cancellationToken)
    {
        if (GetExternalId(lessonId) is not { } eventId)
        {
            return WrongCalendar<LessonState>();
        }

        try
        {
            await eventClient.UpdateLessonStateAsync(options.CalendarId, eventId, state, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<LessonState>.Failure(
                state,
                new ValidationError(
                    "Calendar.UpdateFailed",
                    "Google Calendar could not be updated. Refresh Calendar and try again."));
        }

        return OperationResult<LessonState>.Success(state);
    }

    private CalendarLesson CreateCalendarLesson(
        string eventId, string studentShortName, DateTimeOffset startsAt, LessonState state, bool hasKnownStartTime) =>
        new(CreateScopedId(eventId), studentShortName, startsAt, state, hasKnownStartTime,
            options.CalendarScope is null ? null : eventId);

    private string CreateScopedId(string eventId) =>
        options.CalendarScope is { } scope ? $"{scope}:{eventId}" : eventId;

    private string? GetExternalId(string lessonId)
    {
        if (options.CalendarScope is not { } scope)
        {
            return lessonId;
        }

        var prefix = $"{scope}:";
        return lessonId.StartsWith(prefix, StringComparison.Ordinal) && lessonId.Length > prefix.Length
            ? lessonId[prefix.Length..]
            : null;
    }

    private static OperationResult<T> WrongCalendar<T>() =>
        OperationResult<T>.Failure(new ValidationError(
            "Calendar.ScopeMismatch", "This lesson belongs to a different calendar. Refresh the dashboard or switch to the original calendar."));

    private static OperationResult<IReadOnlyList<CalendarLesson>> CalendarUpdateFailure() =>
        OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
            new ValidationError(
                "Calendar.UpdateFailed",
                "Google Calendar could not be updated. Refresh Calendar and try again."));

    private static OperationResult<IReadOnlyList<CalendarLesson>> CalendarRefreshFailure() =>
        OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
            [],
            new ValidationError(
                "Calendar.RefreshFailed",
                "Google Calendar was updated, but the lesson list could not be refreshed."));

    private async Task<OperationResult<IReadOnlyList<CalendarLesson>>> RefreshAfterSuccessfulWriteAsync(
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        try
        {
            return OperationResult<IReadOnlyList<CalendarLesson>>.Success(
                await ListRefreshMonthAsync(refreshMonth, cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return CalendarRefreshFailure();
        }
    }

    private async Task<OperationResult<IReadOnlyList<CalendarLesson>>> RefreshAfterSuccessfulPopulateAsync(
        DateOnly refreshMonth,
        IReadOnlyList<CalendarLesson> createdLessons,
        CancellationToken cancellationToken)
    {
        try
        {
            return OperationResult<IReadOnlyList<CalendarLesson>>.Success(
                await ListRefreshMonthAsync(refreshMonth, cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
                createdLessons,
                new ValidationError(
                    "Calendar.RefreshFailed",
                    "Google Calendar was updated, but the lesson list could not be refreshed."));
        }
    }

    private async Task<IReadOnlyList<CalendarLesson>> ListRefreshMonthAsync(DateOnly refreshMonth, CancellationToken cancellationToken)
    {
        var firstOfMonth = new DateOnly(refreshMonth.Year, refreshMonth.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        return await ListLessonsAsync(firstOfMonth, lastOfMonth, cancellationToken);
    }

    private DateTimeOffset StartOfDay(DateOnly date)
    {
        var localDateTime = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(localDateTime, options.TimeZone.GetUtcOffset(localDateTime));
    }
}
