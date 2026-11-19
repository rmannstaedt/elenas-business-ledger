using System.Windows;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;

namespace MySmallBusinessLedger.App.Views;

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
