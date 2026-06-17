namespace MySmallBusinessLedger.Core.Plans;

public sealed record PlanCatalogItem(
    int Id,
    string Name,
    decimal PriceWithoutVat,
    LessonsPerWeek LessonsPerWeek,
    int DurationMinutes,
    PlanFlexibility Flexibility,
    int NumberOfStudents,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? RetireOn,
    PlanCatalogState State)
{
    public bool CanBeAssigned => State == PlanCatalogState.Active;

    public bool IsImmutable => State is PlanCatalogState.Active or PlanCatalogState.Retired;
}
