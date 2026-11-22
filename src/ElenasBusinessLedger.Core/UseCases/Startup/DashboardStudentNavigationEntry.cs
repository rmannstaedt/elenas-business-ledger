namespace ElenasBusinessLedger.Core.UseCases.Startup;

// Identifies the student and appointment or schedule time used to move through the dashboard.
public sealed record DashboardStudentNavigationEntry(
    string StudentShortName,
    DateTimeOffset StartsAt,
    bool HasCalendarLesson = true,
    int? StudentId = null);
