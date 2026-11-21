using ElenasBusinessLedger.Core.Common;

namespace ElenasBusinessLedger.Core.Calendar;

public enum CalendarWriteState
{
    Acknowledged,
    Rejected,
    Uncertain
}

// Carries a calendar write outcome and any list already supplied by the gateway.
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

}
