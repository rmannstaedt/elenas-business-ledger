using System.Windows;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;

namespace MySmallBusinessLedger.App.Views;

public partial class PlanCatalogView : Window
{
    public PlanCatalogView(PlanCatalogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowPlacementStore.Attach(this, nameof(PlanCatalogView));
        DataGridColumnWidthStore.Attach(PlansGrid, nameof(PlanCatalogView) + ".PlansGrid");
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
