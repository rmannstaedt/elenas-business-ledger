using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.UseCases.Startup;

public sealed record StartupDashboardResult(
    string? CurrentStudentShortName,
    IReadOnlyList<string> StudentShortNames,
    IReadOnlyList<DashboardLesson> Lessons,
    IReadOnlyList<IssuedInvoiceSummary> Invoices,
    bool CurrentStudentHasSchedule,
    IReadOnlyList<DashboardStudentNavigationEntry> StudentNavigationEntries,
    IReadOnlyList<DashboardPlanOption>? PlanOptions = null,
    DashboardStudentLessonDefaults? LessonDefaults = null);
