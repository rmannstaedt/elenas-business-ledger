using MySmallBusinessLedger.Core.Invoices;

namespace MySmallBusinessLedger.Core.UseCases.Startup;

public sealed record StartupDashboardResult(
    string? CurrentStudentShortName,
    IReadOnlyList<string> StudentShortNames,
    IReadOnlyList<DashboardLesson> Lessons,
    IReadOnlyList<IssuedInvoiceSummary> Invoices,
    bool CurrentStudentHasSchedule,
    IReadOnlyList<DashboardStudentNavigationEntry> StudentNavigationEntries,
    IReadOnlyList<DashboardPlanOption>? PlanOptions = null,
    DashboardStudentLessonDefaults? LessonDefaults = null);
