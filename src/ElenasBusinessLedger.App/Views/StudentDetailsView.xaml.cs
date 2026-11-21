using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.ViewModels;

namespace ElenasBusinessLedger.App.Views;

public partial class StudentDetailsView : Window
{
    private bool closeWasConfirmed;

    public StudentDetailsView(StudentOverviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowPlacementStore.Attach(this, nameof(StudentDetailsView), rememberSize: false);
        viewModel.DetailsCloseRequested += OnDetailsCloseRequested;
        Closing += OnClosing;
        Closed += (_, _) => viewModel.DetailsCloseRequested -= OnDetailsCloseRequested;
    }

    public void CloseWithoutPrompt()
    {
        closeWasConfirmed = true;
        if (IsVisible)
        {
            Close();
        }
    }

    private void OnInvoiceDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is StudentOverviewViewModel viewModel &&
            sender is DataGrid { SelectedItem: InvoiceDisplayModel invoice })
        {
            viewModel.OpenInvoiceCommand.Execute(invoice);
        }
    }

    private void OnDetailsCloseRequested(object? sender, EventArgs e) => CloseWithoutPrompt();

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (closeWasConfirmed || DataContext is not StudentOverviewViewModel viewModel)
        {
            return;
        }

        if (viewModel.IsBusy)
        {
            e.Cancel = true;
            return;
        }

        if (!viewModel.HasUnsavedStudentChanges)
        {
            return;
        }

        e.Cancel = true;
        if (!await viewModel.PrepareForDetailsWindowCloseAsync())
        {
            return;
        }

        closeWasConfirmed = true;
        _ = Dispatcher.BeginInvoke(new Action(Close));
    }
}
