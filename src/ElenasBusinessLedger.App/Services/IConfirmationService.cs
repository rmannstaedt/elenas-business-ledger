using ElenasBusinessLedger.App.ViewModels;

namespace ElenasBusinessLedger.App.Services;

// Gives the dashboard a way to confirm sensitive actions before changing lessons or invoice payment status.
public interface IConfirmationService
{
    Task<bool> ConfirmMarkInvoiceOutstandingAsync(
        InvoiceDisplayModel invoice,
        CancellationToken cancellationToken);

    Task<bool> ConfirmCalendarRemovalAsync(LessonDisplayModel lesson, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    Task<bool> ConfirmDeleteLessonAsync(
        LessonDisplayModel lesson,
        CancellationToken cancellationToken);

    Task<bool> ConfirmMarkLessonPlannedAsync(
        LessonDisplayModel lesson,
        CancellationToken cancellationToken);
}
