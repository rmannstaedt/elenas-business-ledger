namespace ElenasBusinessLedger.Core.UseCases.Startup;

public sealed record DashboardStudentNavigationEntry(
    string StudentShortName,
    DateTimeOffset StartsAt,
    bool HasCalendarLesson = true,
    int? StudentId = null);
