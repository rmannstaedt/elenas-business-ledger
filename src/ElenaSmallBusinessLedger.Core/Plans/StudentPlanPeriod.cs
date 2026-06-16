namespace ElenaSmallBusinessLedger.Core.Plans;

public sealed record StudentPlanPeriod(Plan Plan, DateOnly StartsOn, DateOnly? EndsOn)
{
    public static Plan? GetActivePlan(IEnumerable<StudentPlanPeriod> periods, DateOnly date)
    {
        return periods
            .Where(period => period.StartsOn <= date && (period.EndsOn is null || period.EndsOn >= date))
            .OrderByDescending(period => period.StartsOn)
            .Select(period => period.Plan)
            .FirstOrDefault();
    }
}
