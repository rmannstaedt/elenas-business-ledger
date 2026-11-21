using ElenasBusinessLedger.Core.Plans;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

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
