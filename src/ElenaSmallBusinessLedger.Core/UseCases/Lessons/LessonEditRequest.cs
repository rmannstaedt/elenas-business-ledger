namespace ElenaSmallBusinessLedger.Core.UseCases.Lessons;

public sealed record LessonEditRequest(string LessonId, string StudentShortName, DateTimeOffset StartsAt);
