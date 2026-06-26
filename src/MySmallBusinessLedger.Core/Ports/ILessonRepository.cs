using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.Invoices;

namespace MySmallBusinessLedger.Core.Ports;

public interface ILessonRepository
{
    Task<LessonRecord> SaveAsync(LessonRecord lesson, CancellationToken cancellationToken);

    Task<IReadOnlyList<LessonRecord>> ListForStudentMonthAsync(
        string studentShortName,
        DateOnly month,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LessonRecord>> ListForWindowAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task MarkInvoicedAsync(
        IReadOnlyList<int> lessonIds,
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken);

    Task<LessonRecord?> UpdatePlanAsync(
        int lessonId,
        int planCatalogId,
        string planName,
        int durationMinutes,
        CancellationToken cancellationToken);

    Task DeleteUninvoicedAsync(IReadOnlyList<int> lessonIds, CancellationToken cancellationToken);
}
