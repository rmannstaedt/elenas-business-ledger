using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Lessons;

public sealed class DeleteLessonUseCase
{
    private readonly IGoogleCalendarGateway calendarGateway;

    public DeleteLessonUseCase(IGoogleCalendarGateway calendarGateway)
    {
        this.calendarGateway = calendarGateway;
    }

    public Task<OperationResult<IReadOnlyList<CalendarLesson>>> ExecuteAsync(
        string lessonId,
        DateOnly refreshMonth,
        CancellationToken cancellationToken) =>
        calendarGateway.DeleteLessonAsync(lessonId, refreshMonth, cancellationToken);
}
