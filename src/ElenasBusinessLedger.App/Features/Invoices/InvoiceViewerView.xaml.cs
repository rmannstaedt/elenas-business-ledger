using System.Windows;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.ViewModels;

namespace ElenasBusinessLedger.App.Views;

// Displays an issued invoice and its print information from the read-only viewer model.
public partial class InvoiceViewerView : Window
{
    public InvoiceViewerView(InvoiceViewerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowPlacementStore.Attach(this, nameof(InvoiceViewerView));
        GridWidthStore.Attach(this, nameof(InvoiceViewerView), [InvoiceLinesGrid]);
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
