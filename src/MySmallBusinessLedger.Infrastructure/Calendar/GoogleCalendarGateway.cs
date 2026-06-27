using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.UseCases.Lessons;

namespace MySmallBusinessLedger.Infrastructure.Calendar;

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
                return new CalendarLesson(
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
                StringComparer.OrdinalIgnoreCase.Equals(lesson.Id, createdLessonId)) ??
                new CalendarLesson(
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
                    new CalendarLesson(
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

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> PopulateLessonsAsync(
        IReadOnlyList<LessonAddRequest> requests,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var request in requests)
            {
                await eventClient.CreateLessonEventAsync(
                    options.CalendarId,
                    $"$ {request.StudentShortName}",
                    request.StartsAt,
                    request.HasKnownStartTime,
                    options.DefaultLessonDuration,
                    cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return CalendarUpdateFailure();
        }

        return await RefreshAfterSuccessfulWriteAsync(refreshMonth, cancellationToken);
    }

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> UpdateLessonAsync(
        LessonEditRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        try
        {
            await eventClient.MoveEventAsync(
                options.CalendarId,
                request.LessonId,
                request.StartsAt,
                request.HasKnownStartTime,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return CalendarUpdateFailure();
        }

        return await RefreshAfterSuccessfulWriteAsync(refreshMonth, cancellationToken);
    }

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteLessonAsync(
        string lessonId,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        try
        {
            await eventClient.DeleteEventAsync(options.CalendarId, lessonId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return CalendarUpdateFailure();
        }

        return await RefreshAfterSuccessfulWriteAsync(refreshMonth, cancellationToken);
    }

    public async Task<OperationResult<LessonState>> UpdateLessonStateAsync(
        string lessonId,
        LessonState state,
        CancellationToken cancellationToken)
    {
        try
        {
            await eventClient.UpdateLessonStateAsync(options.CalendarId, lessonId, state, cancellationToken);
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
