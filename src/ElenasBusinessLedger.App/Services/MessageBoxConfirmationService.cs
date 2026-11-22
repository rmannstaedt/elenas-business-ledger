using System.Windows;
using ElenasBusinessLedger.App.ViewModels;

namespace ElenasBusinessLedger.App.Services;

// Abstracts the WPF message box so confirmation behavior can be checked without showing a real dialog.
public interface IMessageBoxPresenter
{
    MessageBoxResult Show(
        Window? owner,
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxImage icon,
        MessageBoxResult defaultResult);
}

// Implements confirmation prompts with the native WPF message box.
public sealed class WpfMessageBoxPresenter : IMessageBoxPresenter
{
    public MessageBoxResult Show(
        Window? owner,
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxImage icon,
        MessageBoxResult defaultResult) =>
        owner is null
            ? MessageBox.Show(messageBoxText, caption, button, icon, defaultResult)
            : MessageBox.Show(owner, messageBoxText, caption, button, icon, defaultResult);
}

// Presents dashboard confirmations with the relevant lesson or invoice details.
public sealed class MessageBoxConfirmationService : IConfirmationService
{
    private readonly Func<Window?> ownerProvider;
    private readonly IMessageBoxPresenter presenter;

    public MessageBoxConfirmationService()
        : this(() => Application.Current?.MainWindow, new WpfMessageBoxPresenter())
    {
    }

    public MessageBoxConfirmationService(Func<Window?> ownerProvider, IMessageBoxPresenter presenter)
    {
        this.ownerProvider = ownerProvider;
        this.presenter = presenter;
    }

    public Task<bool> ConfirmMarkInvoiceOutstandingAsync(
        InvoiceDisplayModel invoice,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = presenter.Show(
            ownerProvider(),
            $"Mark invoice {invoice.InvoiceNumberText} as outstanding?",
            "Confirm payment status",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task<bool> ConfirmCalendarRemovalAsync(LessonDisplayModel lesson, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var answer = MessageBox.Show(
            $"Check the original calendar for the lesson for {lesson.StudentShortName} on {lesson.StartsAtText}, including moved appointments. Close any other copy of the Ledger before continuing. Have you confirmed the appointment was removed?",
            "Review calendar change", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        return Task.FromResult(answer == MessageBoxResult.Yes);
    }

    public Task<bool> ConfirmDeleteLessonAsync(
        LessonDisplayModel lesson,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = presenter.Show(
            ownerProvider(),
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

        var result = presenter.Show(
            ownerProvider(),
            $"Mark the lesson for {lesson.StudentShortName} on {lesson.StartsAtText} as planned again?",
            "Confirm lesson status",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        return Task.FromResult(result == MessageBoxResult.Yes);
    }
}
