using MySmallBusinessLedger.Core.Plans;

namespace MySmallBusinessLedger.Core.UseCases.Plans;

public sealed record PlanDraftInput(
    string Name,
    decimal PriceWithoutVat,
    LessonsPerWeek LessonsPerWeek,
    int DurationMinutes,
    PlanFlexibility Flexibility,
    int NumberOfStudents,
    DateTimeOffset EffectiveFrom);
