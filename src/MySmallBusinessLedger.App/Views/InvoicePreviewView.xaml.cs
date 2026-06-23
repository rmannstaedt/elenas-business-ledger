using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;

namespace MySmallBusinessLedger.App.Views;

public partial class InvoicePreviewView : Window
{
    public InvoicePreviewView(InvoicePreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
        WindowPlacementStore.Attach(this, nameof(InvoicePreviewView));
    }

    private void OnCloseRequested(object? sender, bool? dialogResult)
    {
        DialogResult = dialogResult;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
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
