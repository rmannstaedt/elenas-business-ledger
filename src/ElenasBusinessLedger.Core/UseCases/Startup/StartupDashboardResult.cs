using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Startup;

// Returns the dashboard selection, lesson rows, invoice history, and editor defaults from one refresh.
public sealed record StartupDashboardResult(
    string? CurrentStudentShortName,
    IReadOnlyList<string> StudentShortNames,
    IReadOnlyList<DashboardLesson> Lessons,
    IReadOnlyList<IssuedInvoiceSummary> Invoices,
    bool CurrentStudentHasSchedule,
    IReadOnlyList<DashboardStudentNavigationEntry> StudentNavigationEntries,
    IReadOnlyList<DashboardPlanOption>? PlanOptions = null,
    DashboardStudentLessonDefaults? LessonDefaults = null,
    int? CurrentStudentId = null,
    IReadOnlyList<StudentChoice>? StudentChoices = null);
