namespace ElenasBusinessLedger.Core.Plans;

// Captures the terms assigned for a student period while retaining the originating catalog plan ID.
public sealed class Plan
{
    public Plan(
        string name,
        decimal priceWithoutVat,
        LessonsPerWeek lessonsPerWeek,
        int durationMinutes,
        PlanFlexibility flexibility,
        int numberOfStudents,
        DateTimeOffset effectiveFrom,
        string paymentTerms = "",
        int planCatalogId = 0)
    {
        PlanCatalogId = planCatalogId;
        Name = name;
        PriceWithoutVat = priceWithoutVat;
        LessonsPerWeek = lessonsPerWeek;
        DurationMinutes = durationMinutes;
        Flexibility = flexibility;
        NumberOfStudents = numberOfStudents;
        EffectiveFrom = effectiveFrom;
        PaymentTerms = paymentTerms;
    }

    public int PlanCatalogId { get; }

    public string Name { get; }

    public decimal PriceWithoutVat { get; }

    public LessonsPerWeek LessonsPerWeek { get; }

    public int DurationMinutes { get; }

    public PlanFlexibility Flexibility { get; }

    public int NumberOfStudents { get; }

    public DateTimeOffset EffectiveFrom { get; }

    public string PaymentTerms { get; }
}
