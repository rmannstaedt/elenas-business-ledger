using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Lessons;

public sealed class AddLessonUseCase
{
    private readonly IGoogleCalendarGateway calendarGateway;

    public AddLessonUseCase(IGoogleCalendarGateway calendarGateway)
    {
        this.calendarGateway = calendarGateway;
    }

    public Task<OperationResult<IReadOnlyList<CalendarLesson>>> ExecuteAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken) =>
        calendarGateway.AddLessonAsync(request, refreshMonth, cancellationToken);
}
