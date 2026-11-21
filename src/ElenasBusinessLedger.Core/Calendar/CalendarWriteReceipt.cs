using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.Calendar;

public enum CalendarWriteState
{
    Acknowledged,
    Rejected,
    Uncertain
}

// Separates the outcome of a calendar write from the outcome of the dashboard refresh.
public sealed record CalendarWriteReceipt(
    CalendarWriteState State,
    IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<CalendarLesson>? RefreshedLessons = null)
{
    // Adapts write-and-refresh results so a failed refresh does not turn an acknowledged write into an uncertain one.
    public static CalendarWriteReceipt FromPreviousResult(OperationResult<IReadOnlyList<CalendarLesson>> result)
    {
        if (result.Succeeded || result.Errors.Any(error => error.Code == "Calendar.RefreshFailed"))
        {
            return new(CalendarWriteState.Acknowledged, result.Errors, result.Value ?? []);
        }

        var state = result.Errors.Any(error => error.Code is "Calendar.NotConfigured" or "Calendar.ScopeMismatch")
            ? CalendarWriteState.Rejected : CalendarWriteState.Uncertain;
        return new(state, result.Errors);
    }

    // Gets the current month for the dashboard while preserving any refresh result already attached to the receipt.
    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> RefreshAsync(
        IGoogleCalendarGateway gateway, DateOnly month, CancellationToken cancellationToken)
    {
        if (RefreshedLessons is not null)
        {
            return Errors.Count == 0
                ? OperationResult<IReadOnlyList<CalendarLesson>>.Success(RefreshedLessons)
                : OperationResult<IReadOnlyList<CalendarLesson>>.Failure(RefreshedLessons, Errors.ToArray());
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
