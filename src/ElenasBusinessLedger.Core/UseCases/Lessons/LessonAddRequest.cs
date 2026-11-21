namespace ElenasBusinessLedger.Core.UseCases.Lessons;

// Carries the student's identity, chosen plan, and time needed to create and link a lesson.
public sealed record LessonAddRequest(
    string StudentShortName,
    DateTimeOffset StartsAt,
    int PlanCatalogId,
    bool HasKnownStartTime = true,
    int? StudentId = null);
