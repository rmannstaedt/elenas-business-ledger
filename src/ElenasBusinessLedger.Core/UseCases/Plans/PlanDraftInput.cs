using ElenasBusinessLedger.Core.Plans;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

// Carries editable catalog terms for use cases that create or revise an unpublished plan.
public sealed record PlanDraftInput(
    string Name,
    decimal? PriceWithoutVat,
    LessonsPerWeek LessonsPerWeek,
    int DurationMinutes,
    PlanFlexibility Flexibility,
    int NumberOfStudents,
    DateTimeOffset EffectiveFrom,
    string PaymentTerms,
    DateTimeOffset? RetireOn);
