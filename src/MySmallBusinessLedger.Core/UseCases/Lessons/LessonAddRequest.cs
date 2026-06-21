namespace MySmallBusinessLedger.Core.UseCases.Lessons;

public sealed record LessonAddRequest(string StudentShortName, DateTimeOffset StartsAt);
