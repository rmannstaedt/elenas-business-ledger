using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Lessons;
using ElenaSmallBusinessLedger.Core.UseCases.Startup;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class SampleStartupDashboardService : IStartupDashboardService
{
    public Task<OperationResult<StartupDashboardResult>> LoadAsync(
        DateOnly month,
        string? selectedStudentShortName,
        CancellationToken cancellationToken)
    {
        var lessonMonth = new DateOnly(month.Year, month.Month, 1);
        var student = selectedStudentShortName ?? "Anna";

        var result = new StartupDashboardResult(
            student,
            ["Anna", "Bo", "Cecilie"],
            [
                new DashboardLesson(
                    "lesson-previous",
                    student,
                    new DateTimeOffset(lessonMonth.AddMonths(-1).ToDateTime(new TimeOnly(15, 0)), TimeSpan.FromHours(2)),
                    LessonState.Completed,
                    IsInvoiced: true,
                    PaymentStatus.Paid),
                new DashboardLesson(
                    "lesson-current",
                    student,
                    new DateTimeOffset(lessonMonth.AddDays(15).ToDateTime(new TimeOnly(15, 0)), TimeSpan.FromHours(2)),
                    LessonState.Completed,
                    IsInvoiced: false,
                    PaymentStatus: null),
                new DashboardLesson(
                    "lesson-next",
                    student,
                    new DateTimeOffset(lessonMonth.AddMonths(1).AddDays(6).ToDateTime(new TimeOnly(15, 0)), TimeSpan.FromHours(2)),
                    LessonState.Planned,
                    IsInvoiced: false,
                    PaymentStatus: null)
            ],
            [
                new IssuedInvoiceSummary(
                    new InvoiceNumber(1),
                    student,
                    lessonMonth.AddDays(15),
                    PaymentStatus.Outstanding,
                    @"C:\Invoices\000001-Anna.pdf")
            ]);

        return Task.FromResult(OperationResult<StartupDashboardResult>.Success(result));
    }
}
