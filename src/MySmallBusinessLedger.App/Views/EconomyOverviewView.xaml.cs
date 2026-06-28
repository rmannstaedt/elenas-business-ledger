using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;

namespace MySmallBusinessLedger.App.Views;

public partial class EconomyOverviewView : Window
{
    private const double NormalMinHeight = 720d;
    private const double ErrorMinHeight = 760d;
    private const double ErrorHeight = 800d;
    private bool heightExpandedForError;
    private double heightBeforeError = 760d;

    public EconomyOverviewView(EconomyOverviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdateErrorSizing(viewModel.HasError);
        WindowPlacementStore.Attach(this, nameof(EconomyOverviewView));
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EconomyOverviewViewModel.HasError) &&
            sender is EconomyOverviewViewModel viewModel)
        {
            UpdateErrorSizing(viewModel.HasError);
        }
    }

    private void UpdateErrorSizing(bool hasError)
    {
        if (hasError)
        {
            MinHeight = ErrorMinHeight;
            if (WindowState == WindowState.Normal && Height < ErrorHeight)
            {
                heightBeforeError = Height;
                Height = ErrorHeight;
                heightExpandedForError = true;
            }

            return;
        }

        MinHeight = NormalMinHeight;
        if (heightExpandedForError && WindowState == WindowState.Normal)
        {
            Height = Math.Max(NormalMinHeight, heightBeforeError);
        }

        heightExpandedForError = false;
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

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();
}
