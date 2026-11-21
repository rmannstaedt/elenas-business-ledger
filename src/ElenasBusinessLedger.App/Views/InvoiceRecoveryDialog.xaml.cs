using System.Windows;
using ElenasBusinessLedger.Core.UseCases.Invoices;

namespace ElenasBusinessLedger.App.Views;

public partial class InvoiceRecoveryDialog : Window
{
    public InvoiceRecoveryDialog(PendingInvoiceRecovery recovery, bool directRecovery = false, bool canDiscard = false)
    {
        InitializeComponent();
        DataContext = recovery;
        var invoice = recovery.SavedInvoice;
        var legacy = invoice.SnapshotRevision is null;
        Title = $"Resume invoice {invoice.InvoiceNumber}";
        Instructions.Text = legacy
            ? "The original adjustment details are unavailable. Review the saved invoice and check the original invoice folder before using the current preview."
            : recovery.CanRevise
                ? "Resume the saved invoice or apply the current preview to the same invoice number."
                : "PDF publication has started. Review the saved invoice before completing it.";
        if (directRecovery)
        {
            Instructions.Text = legacy
                ? "This older reservation requires review of its original invoice details. Discard it only after confirming that no PDF was published."
                : canDiscard ? "Resume the saved invoice or discard its unused reservation."
                    : "PDF publication has started. Review the saved invoice before completing it.";
        }
        SavedDates.Text = $"Dated {invoice.InvoiceDate:d} • Period {invoice.Period.From:d} to {invoice.Period.To:d}";
        SavedAdjustment.Text = invoice.PriceAdjustment is { } adjustment
            ? $"Saved adjustment {adjustment.Amount.Amount:N2} {adjustment.Amount.Currency} ({adjustment.Reason})"
            : string.Empty;
        if (invoice.PriceAdjustment is null)
        {
            SavedAdjustment.Visibility = Visibility.Collapsed;
        }

        SavedTotal.Text = $"Saved total {invoice.Totals.Total.Amount:N2} {invoice.Totals.Total.Currency}";
        CurrentTotal.Text = $"Current preview total {recovery.RequestedTotals.Total.Amount:N2} {recovery.RequestedTotals.Total.Currency}";
        if (recovery.RequestedDraft?.PriceAdjustment is { } requestedAdjustment)
        {
            CurrentAdjustment.Text = $"Current adjustment {requestedAdjustment.Amount.Amount:N2} {requestedAdjustment.Amount.Currency} ({requestedAdjustment.Reason})";
        }
        else
        {
            CurrentAdjustment.Visibility = Visibility.Collapsed;
        }
        ResumeButton.Visibility = legacy ? Visibility.Collapsed : Visibility.Visible;
        ReviseButton.Visibility = recovery.CanRevise ? Visibility.Visible : Visibility.Collapsed;
        LegacyConfirmation.Visibility = legacy && (recovery.CanRevise || canDiscard) ? Visibility.Visible : Visibility.Collapsed;
        ReviseButton.IsEnabled = !legacy;
        DiscardButton.Visibility = canDiscard ? Visibility.Visible : Visibility.Collapsed;
        DiscardButton.IsEnabled = !legacy;
        if (directRecovery)
        {
            CurrentTotal.Visibility = Visibility.Collapsed;
            CurrentAdjustment.Visibility = Visibility.Collapsed;
        }
    }

    public InvoiceRecoveryChoice Choice { get; private set; }

    private void OnLegacyConfirmationChanged(object sender, RoutedEventArgs e)
    {
        ReviseButton.IsEnabled = LegacyConfirmation.IsChecked == true;
        DiscardButton.IsEnabled = LegacyConfirmation.IsChecked == true;
    }

    private void OnDiscard(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Discard the saved draft and release its lessons?", Title,
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
        {
            Choice = InvoiceRecoveryChoice.Discard;
            DialogResult = true;
        }
    }

    private void OnResume(object sender, RoutedEventArgs e)
    {
        Choice = InvoiceRecoveryChoice.Resume;
        DialogResult = true;
    }

    private void OnRevise(object sender, RoutedEventArgs e)
    {
        Choice = InvoiceRecoveryChoice.Revise;
        DialogResult = true;
    }
}
