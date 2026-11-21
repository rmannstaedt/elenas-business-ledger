namespace ElenasBusinessLedger.Core.UseCases.Lessons;

// Identifies the existing appointment and its proposed time so editing moves the correct event.
public sealed record LessonEditRequest(
    string LessonId,
    string StudentShortName,
    DateTimeOffset StartsAt,
    bool HasKnownStartTime = true);
