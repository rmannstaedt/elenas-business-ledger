using System.Windows;
using System.Windows.Controls;
using ElenaSmallBusinessLedger.App.ViewModels;

namespace ElenaSmallBusinessLedger.App;

public partial class MainWindow : Window
{
    private bool hasLoaded;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (hasLoaded)
        {
            return;
        }

        hasLoaded = true;
        await ViewModel.LoadAsync();
    }

    private async void OnActivated(object? sender, EventArgs e)
    {
        if (!hasLoaded)
        {
            return;
        }

        await ViewModel.RefreshAfterActivationAsync();
    }

    private async void OnInvoiceDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGrid { SelectedItem: InvoiceDisplayModel invoice })
        {
            await ViewModel.OpenInvoiceCommand.ExecuteAsync(invoice);
        }
    }

    private async void OnLessonDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGrid { SelectedItem: LessonDisplayModel lesson })
        {
            await ViewModel.EditLessonCommand.ExecuteAsync(lesson);
        }
    }
}
