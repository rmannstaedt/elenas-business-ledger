namespace ElenasBusinessLedger.Core.UseCases.Lessons;

public sealed record LessonAddRequest(
    string StudentShortName,
    DateTimeOffset StartsAt,
    int PlanCatalogId,
    bool HasKnownStartTime = true);
