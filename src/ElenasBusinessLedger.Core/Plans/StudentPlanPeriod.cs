namespace ElenasBusinessLedger.Core.Plans;

// Ties an assigned plan to a date range so a lesson can use the terms in effect on its date.
public sealed record StudentPlanPeriod(Plan Plan, DateOnly StartsOn, DateOnly? EndsOn)
{
    // Finds the plan in effect on a lesson date so schedule and billing rules follow the right period.
    public static Plan? GetActivePlan(IEnumerable<StudentPlanPeriod> periods, DateOnly date)
    {
        return periods
            .Where(period => period.StartsOn <= date && (period.EndsOn is null || period.EndsOn >= date))
            .OrderByDescending(period => period.StartsOn)
            .Select(period => period.Plan)
            .FirstOrDefault();
    }
}
