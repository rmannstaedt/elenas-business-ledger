using System.Windows;
using ElenaSmallBusinessLedger.App.Services;
using ElenaSmallBusinessLedger.App.ViewModels;

namespace ElenaSmallBusinessLedger.App.Views;

public partial class InvoiceViewerView : Window
{
    public InvoiceViewerView(InvoiceViewerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowPlacementStore.Attach(this, nameof(InvoiceViewerView));
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
