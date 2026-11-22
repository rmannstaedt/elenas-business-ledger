using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

// Resolves a held calendar change from an observed appointment or an explicitly confirmed removal.
public sealed class ReviewCalendarMutationUseCase(IGoogleCalendarGateway calendarGateway, ILessonRepository lessonRepository)
{
    // Resolves a pending calendar change from the observed event or an explicitly confirmed removal.
    public async Task<OperationResult<bool>> ExecuteAsync(
        string eventId, DateOnly month, bool confirmRemoved, CancellationToken cancellationToken)
    {
        var from = new DateOnly(month.Year, month.Month, 1);
        var nearby = await calendarGateway.ListLessonsAsync(from.AddMonths(-1), from.AddMonths(2).AddDays(-1), cancellationToken);
        var observed = nearby.FirstOrDefault(lesson => lesson.Id == eventId);
        if (observed is null && !confirmRemoved)
        {
            return OperationResult<bool>.ConfirmationRequired(false, new ValidationError(
                "Lesson.ConfirmCalendarRemoval", "The appointment was outside the retrieved calendar results. Check its original calendar before removing the local billing row."));
        }

        return await lessonRepository.ResolveCalendarMutationAsync(eventId, observed, cancellationToken);
    }
}
