using ElenasBusinessLedger.App.ViewModels;

namespace ElenasBusinessLedger.App.Services;

public interface IConfirmationService
{
    Task<bool> ConfirmMarkInvoiceOutstandingAsync(
        InvoiceDisplayModel invoice,
        CancellationToken cancellationToken);

    Task<bool> ConfirmDeleteLessonAsync(
        LessonDisplayModel lesson,
        CancellationToken cancellationToken);

    Task<bool> ConfirmMarkLessonPlannedAsync(
        LessonDisplayModel lesson,
        CancellationToken cancellationToken);
}
