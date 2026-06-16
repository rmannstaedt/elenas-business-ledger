using ElenaSmallBusinessLedger.Core.Calendar;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Lessons;
using ElenaSmallBusinessLedger.Core.Ports;
using ElenaSmallBusinessLedger.Core.UseCases.Lessons;
using ElenaSmallBusinessLedger.Infrastructure.Calendar;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed record ConfiguredGoogleCalendarClient(string CalendarId, IGoogleCalendarEventClient EventClient);

public sealed class ConfiguredGoogleCalendarGateway : IGoogleCalendarGateway
{
    private readonly Func<CancellationToken, Task<ConfiguredGoogleCalendarClient?>> loadClientAsync;
    private readonly TimeZoneInfo timeZone;
    private readonly TimeSpan defaultLessonDuration;

    public ConfiguredGoogleCalendarGateway(
        Func<CancellationToken, Task<ConfiguredGoogleCalendarClient?>> loadClientAsync,
        TimeZoneInfo timeZone,
        TimeSpan defaultLessonDuration)
    {
        this.loadClientAsync = loadClientAsync;
        this.timeZone = timeZone;
        this.defaultLessonDuration = defaultLessonDuration;
    }

    public async Task<IReadOnlyList<CalendarLesson>> ListLessonsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var gateway = await CreateGatewayAsync(cancellationToken);
        return await gateway.ListLessonsAsync(from, to, cancellationToken);
    }

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> AddLessonAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        var gateway = await TryCreateGatewayAsync(cancellationToken);
        return gateway is null
            ? CalendarNotConfigured<IReadOnlyList<CalendarLesson>>([])
            : await gateway.AddLessonAsync(request, refreshMonth, cancellationToken);
    }

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> UpdateLessonAsync(
        LessonEditRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        var gateway = await TryCreateGatewayAsync(cancellationToken);
        return gateway is null
            ? CalendarNotConfigured<IReadOnlyList<CalendarLesson>>([])
            : await gateway.UpdateLessonAsync(request, refreshMonth, cancellationToken);
    }

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteLessonAsync(
        string lessonId,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        var gateway = await TryCreateGatewayAsync(cancellationToken);
        return gateway is null
            ? CalendarNotConfigured<IReadOnlyList<CalendarLesson>>([])
            : await gateway.DeleteLessonAsync(lessonId, refreshMonth, cancellationToken);
    }

    public async Task<OperationResult<LessonState>> UpdateLessonStateAsync(
        string lessonId,
        LessonState state,
        CancellationToken cancellationToken)
    {
        var gateway = await TryCreateGatewayAsync(cancellationToken);
        return gateway is null
            ? CalendarNotConfigured(state)
            : await gateway.UpdateLessonStateAsync(lessonId, state, cancellationToken);
    }

    private async Task<GoogleCalendarGateway> CreateGatewayAsync(CancellationToken cancellationToken)
    {
        var gateway = await TryCreateGatewayAsync(cancellationToken);
        if (gateway is null)
        {
            throw new InvalidOperationException(NotConfiguredMessage);
        }

        return gateway;
    }

    private async Task<GoogleCalendarGateway?> TryCreateGatewayAsync(CancellationToken cancellationToken)
    {
        var configured = await loadClientAsync(cancellationToken);
        return configured is null
            ? null
            : new GoogleCalendarGateway(
                configured.EventClient,
                new GoogleCalendarGatewayOptions(configured.CalendarId, timeZone, defaultLessonDuration));
    }

    private static OperationResult<T> CalendarNotConfigured<T>(T currentValue) =>
        OperationResult<T>.Failure(
            currentValue,
            new ValidationError("Calendar.NotConfigured", NotConfiguredMessage));

    private const string NotConfiguredMessage =
        "Google Calendar is not configured. Configure a Google account and calendar before using lesson sync.";
}
