namespace MySmallBusinessLedger.Core.Plans;

public sealed class Plan
{
    public Plan(
        string name,
        decimal priceWithoutVat,
        LessonsPerWeek lessonsPerWeek,
        int durationMinutes,
        PlanFlexibility flexibility,
        int numberOfStudents,
        DateTimeOffset effectiveFrom)
    {
        Name = name;
        PriceWithoutVat = priceWithoutVat;
        LessonsPerWeek = lessonsPerWeek;
        DurationMinutes = durationMinutes;
        Flexibility = flexibility;
        NumberOfStudents = numberOfStudents;
        EffectiveFrom = effectiveFrom;
    }

    public string Name { get; }

    public decimal PriceWithoutVat { get; }

    public LessonsPerWeek LessonsPerWeek { get; }

    public int DurationMinutes { get; }

    public PlanFlexibility Flexibility { get; }

    public int NumberOfStudents { get; }

    public DateTimeOffset EffectiveFrom { get; }
}
