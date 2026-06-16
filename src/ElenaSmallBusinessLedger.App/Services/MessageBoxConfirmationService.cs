using System.Windows;
using ElenaSmallBusinessLedger.App.ViewModels;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class MessageBoxConfirmationService : IConfirmationService
{
    public Task<bool> ConfirmMarkInvoiceOutstandingAsync(
        InvoiceDisplayModel invoice,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = MessageBox.Show(
            $"Mark invoice {invoice.InvoiceNumberText} as outstanding?",
            "Confirm payment status",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task<bool> ConfirmDeleteLessonAsync(
        LessonDisplayModel lesson,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = MessageBox.Show(
            $"Delete the lesson for {lesson.StudentShortName} on {lesson.StartsAtText}?",
            "Confirm lesson deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task<bool> ConfirmMarkLessonPlannedAsync(
        LessonDisplayModel lesson,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = MessageBox.Show(
            $"Mark the lesson for {lesson.StudentShortName} on {lesson.StartsAtText} as planned again?",
            "Confirm lesson status",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        return Task.FromResult(result == MessageBoxResult.Yes);
    }
}
