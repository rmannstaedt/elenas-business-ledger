using ElenaSmallBusinessLedger.Core.Invoices;

namespace ElenaSmallBusinessLedger.Core.UseCases.Startup;

public sealed record StartupDashboardResult(
    string? CurrentStudentShortName,
    IReadOnlyList<DashboardLesson> Lessons,
    IReadOnlyList<IssuedInvoiceSummary> Invoices);
