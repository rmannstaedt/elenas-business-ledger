using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

// Reloads the dashboard month after an acknowledged calendar write is committed to the local ledger.
internal static class CalendarRefreshAfterWrite
{
    public static async Task<OperationResult<IReadOnlyList<CalendarLesson>>> LoadAsync(
        IGoogleCalendarGateway gateway,
        CalendarWriteReceipt receipt,
        DateOnly month,
        CancellationToken cancellationToken)
    {
        if (receipt.RefreshedLessons is { } lessons)
        {
            return receipt.Errors.Count == 0
                ? OperationResult<IReadOnlyList<CalendarLesson>>.Success(lessons)
                : OperationResult<IReadOnlyList<CalendarLesson>>.Failure(lessons, receipt.Errors.ToArray());
        }

        try
        {
            var first = new DateOnly(month.Year, month.Month, 1);
            return OperationResult<IReadOnlyList<CalendarLesson>>.Success(
                await gateway.ListLessonsAsync(first, first.AddMonths(1).AddDays(-1), cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure([], new ValidationError(
                "Calendar.RefreshFailed", "The calendar and local ledger were updated. Refresh the dashboard to see the change."));
        }
    }
}
