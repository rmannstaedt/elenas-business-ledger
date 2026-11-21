namespace ElenasBusinessLedger.Core.Plans;

// Represents the reusable plan terms and lifecycle state that determine whether an editor can assign the plan.
public sealed record PlanCatalogItem(
    int Id,
    string Name,
    decimal? PriceWithoutVat,
    LessonsPerWeek LessonsPerWeek,
    int DurationMinutes,
    PlanFlexibility Flexibility,
    int NumberOfStudents,
    DateTimeOffset EffectiveFrom,
    string PaymentTerms,
    DateTimeOffset? RetireOn,
    PlanCatalogState State)
{
    public bool CanBeAssigned =>
        State == PlanCatalogState.Active &&
        !string.IsNullOrWhiteSpace(Name) &&
        PriceWithoutVat is not null &&
        !string.IsNullOrWhiteSpace(PaymentTerms);

    public bool IsImmutable => State is PlanCatalogState.Active or PlanCatalogState.Retired;
}
