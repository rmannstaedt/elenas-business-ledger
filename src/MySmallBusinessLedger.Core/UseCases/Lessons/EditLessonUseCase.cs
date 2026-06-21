using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Lessons;

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
