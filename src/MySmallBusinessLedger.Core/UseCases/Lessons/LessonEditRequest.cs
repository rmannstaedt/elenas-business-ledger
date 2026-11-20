namespace MySmallBusinessLedger.Core.UseCases.Lessons;

public sealed record LessonEditRequest(
    string LessonId,
    string StudentShortName,
    DateTimeOffset StartsAt,
    bool HasKnownStartTime = true);
