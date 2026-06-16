using ElenaSmallBusinessLedger.Core.Invoices;

namespace ElenaSmallBusinessLedger.Core.UseCases.Startup;

public sealed record StartupDashboardResult(
    string? CurrentStudentShortName,
    IReadOnlyList<string> StudentShortNames,
    IReadOnlyList<DashboardLesson> Lessons,
    IReadOnlyList<IssuedInvoiceSummary> Invoices);
