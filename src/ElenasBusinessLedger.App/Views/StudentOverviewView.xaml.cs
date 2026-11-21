using System.Windows;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.ViewModels;

namespace ElenasBusinessLedger.App.Views;

public partial class StudentOverviewView : Window
{
    private bool closeWasConfirmed;

    public StudentOverviewView(StudentOverviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowPlacementStore.Attach(this, nameof(StudentOverviewView));
        GridWidthStore.Attach(this, nameof(StudentOverviewView), [StudentsGrid]);
        Closing += OnClosing;
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
