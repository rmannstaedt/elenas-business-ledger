using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.ViewModels;

namespace ElenasBusinessLedger.App.Views;

// Presents the draft and handles window closing while the view model issues or reprints the invoice.
public partial class InvoicePreviewView : Window
{
    private bool closeWasConfirmed;

    public InvoicePreviewView(InvoicePreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
        WindowPlacementStore.Attach(this, nameof(InvoicePreviewView));
        GridWidthStore.Attach(this, nameof(InvoicePreviewView), [InvoiceLinesGrid]);
        Closing += OnClosing;
    }

    private void OnCloseRequested(object? sender, bool? dialogResult)
    {
        closeWasConfirmed = true;
        DialogResult = dialogResult;
        Close();
    }

    private async void OnCancel(object sender, RoutedEventArgs e)
    {
        if (DataContext is InvoicePreviewViewModel viewModel &&
            !await viewModel.RequestDismissAsync())
        {
            return;
        }

        closeWasConfirmed = true;
        DialogResult = false;
        Close();
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (closeWasConfirmed ||
            DataContext is not InvoicePreviewViewModel viewModel ||
            viewModel.CanDismiss)
        {
            return;
        }

        e.Cancel = true;
        await viewModel.RequestDismissAsync();
    }

    private void OnIncludeLinePreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not CheckBox { IsEnabled: true } checkBox)
        {
            return;
        }

        e.Handled = true;
        checkBox.IsChecked = checkBox.IsChecked != true;
    }
}
