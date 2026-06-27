using System.Windows;
using System.Windows.Input;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;

namespace MySmallBusinessLedger.App.Views;

public partial class EconomyOverviewView : Window
{
    public EconomyOverviewView(EconomyOverviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowPlacementStore.Attach(this, nameof(EconomyOverviewView));
    }

    private void InvoicesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is EconomyOverviewViewModel viewModel &&
            InvoicesGrid.SelectedItem is EconomyOverviewViewModel.EconomyOverviewInvoiceDisplayModel invoice &&
            viewModel.OpenInvoicePdfCommand.CanExecute(invoice))
        {
            viewModel.OpenInvoicePdfCommand.Execute(invoice);
        }
    }
}
