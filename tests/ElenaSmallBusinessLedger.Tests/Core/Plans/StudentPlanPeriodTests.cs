using ElenaSmallBusinessLedger.Core.Plans;

namespace ElenaSmallBusinessLedger.Tests.Core.Plans;

public sealed class StudentPlanPeriodTests
{
    [Fact]
    public void GetActivePlanUsesCurrentPeriodUntilFuturePlanStarts()
    {
        var currentPlan = new Plan(
            name: "Tilbud 3",
            priceWithoutVat: 400m,
            lessonsPerWeek: LessonsPerWeek.One,
            durationMinutes: 60,
            flexibility: PlanFlexibility.MoveOrCancelByAgreement,
            numberOfStudents: 1,
            effectiveFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var futurePlan = new Plan(
            name: "Tilbud 2",
            priceWithoutVat: 350m,
            lessonsPerWeek: LessonsPerWeek.Two,
            durationMinutes: 60,
            flexibility: PlanFlexibility.MoveOrCancelByAgreement,
            numberOfStudents: 1,
            effectiveFrom: new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));
        var periods = new[]
        {
            new StudentPlanPeriod(currentPlan, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)),
            new StudentPlanPeriod(futurePlan, new DateOnly(2026, 7, 1), null)
        };

        var activePlan = StudentPlanPeriod.GetActivePlan(periods, new DateOnly(2026, 6, 16));

        Assert.Same(currentPlan, activePlan);
    }

    [Fact]
    public void GetActivePlanUsesNewPeriodOnStartDate()
    {
        var oldPlan = new Plan(
            name: "Tilbud 3",
            priceWithoutVat: 400m,
            lessonsPerWeek: LessonsPerWeek.One,
            durationMinutes: 60,
            flexibility: PlanFlexibility.MoveOrCancelByAgreement,
            numberOfStudents: 1,
            effectiveFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var newPlan = new Plan(
            name: "Tilbud 4",
            priceWithoutVat: 450m,
            lessonsPerWeek: LessonsPerWeek.ByAppointment,
            durationMinutes: 60,
            flexibility: PlanFlexibility.MoveOrCancelByAgreement,
            numberOfStudents: 1,
            effectiveFrom: new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));
        var periods = new[]
        {
            new StudentPlanPeriod(oldPlan, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)),
            new StudentPlanPeriod(newPlan, new DateOnly(2026, 7, 1), null)
        };

        var activePlan = StudentPlanPeriod.GetActivePlan(periods, new DateOnly(2026, 7, 1));

        Assert.Same(newPlan, activePlan);
    }
}
