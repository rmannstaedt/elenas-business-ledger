namespace MySmallBusinessLedger.Core.UseCases.Startup;

public sealed record DashboardStudentNavigationEntry(
    string StudentShortName,
    DateTimeOffset StartsAt,
    bool HasCalendarLesson = true);
