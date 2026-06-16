using ElenaSmallBusinessLedger.Core.Calendar;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Ports;

namespace ElenaSmallBusinessLedger.Core.UseCases.Lessons;

public sealed class EditLessonUseCase
{
    private readonly IGoogleCalendarGateway calendarGateway;

    public EditLessonUseCase(IGoogleCalendarGateway calendarGateway)
    {
        this.calendarGateway = calendarGateway;
    }

    public Task<OperationResult<IReadOnlyList<CalendarLesson>>> ExecuteAsync(
        LessonEditRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken) =>
        calendarGateway.UpdateLessonAsync(request, refreshMonth, cancellationToken);
}
