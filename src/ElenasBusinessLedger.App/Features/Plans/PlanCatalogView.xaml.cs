using System.Windows;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.ViewModels;

namespace ElenasBusinessLedger.App.Views;

// Presents plan management and gives the view model a chance to resolve unsaved changes before closing.
public partial class PlanCatalogView : Window
{
    private bool closeWasConfirmed;

    public PlanCatalogView(PlanCatalogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowPlacementStore.Attach(this, nameof(PlanCatalogView));
        GridWidthStore.Attach(this, nameof(PlanCatalogView), [PlansGrid]);
        Closing += OnClosing;
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (closeWasConfirmed || DataContext is not PlanCatalogViewModel viewModel)
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
