using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;

namespace MySmallBusinessLedger.App.Views;

public partial class StudentOverviewView : Window
{
    private bool closeWasConfirmed;

    public StudentOverviewView(StudentOverviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowPlacementStore.Attach(this, nameof(StudentOverviewView));
        DataGridColumnWidthStore.Attach(StudentsGrid, nameof(StudentOverviewView) + ".StudentsGrid");
        DataGridColumnWidthStore.Attach(InvoicesGrid, nameof(StudentOverviewView) + ".InvoicesGrid");
        Closing += OnClosing;
    }

    private void OnInvoiceDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is StudentOverviewViewModel viewModel &&
            sender is DataGrid { SelectedItem: InvoiceDisplayModel invoice })
        {
            viewModel.OpenInvoiceCommand.Execute(invoice);
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (closeWasConfirmed || DataContext is not StudentOverviewViewModel viewModel || !viewModel.HasUnsavedStudentChanges)
        {
            return;
        }

        e.Cancel = true;
        if (!await viewModel.PrepareForCloseAsync())
        {
            return;
        }

        closeWasConfirmed = true;
        _ = Dispatcher.BeginInvoke(new Action(Close));
    }
}
