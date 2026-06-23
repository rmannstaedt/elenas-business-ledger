using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;

namespace MySmallBusinessLedger.App;

public partial class MainWindow : Window
{
    private bool hasLoaded;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowPlacementStore.Attach(this, nameof(MainWindow));
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

    private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.ClearError();
    }

    private async void OnLessonEditCommitted(object sender, RoutedEventArgs e)
    {
        await CommitLessonEditAsync(sender);
    }

    private async void OnLessonCalendarClosed(object? sender, EventArgs e)
    {
        await CommitLessonEditAsync(sender);
    }

    private async void OnLessonTimeDropDownClosed(object? sender, EventArgs e)
    {
        await CommitLessonEditAsync(sender);
    }

    private async Task CommitLessonEditAsync(object? sender)
    {
        if (sender is FrameworkElement { DataContext: LessonDisplayModel lesson })
        {
            await ViewModel.SaveLessonCommand.ExecuteAsync(lesson);
        }
    }

    private void OnLessonTimeSelectorPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ComboBox comboBox || !comboBox.IsEnabled || comboBox.IsKeyboardFocusWithin)
        {
            return;
        }

        e.Handled = true;
        comboBox.Focus();
        comboBox.Dispatcher.BeginInvoke(
            new Action(() => comboBox.IsDropDownOpen = true),
            DispatcherPriority.Input);
    }

    private async void OnDeleteLessonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: LessonDisplayModel lesson } ||
            !ViewModel.DeleteLessonCommand.CanExecute(lesson))
        {
            return;
        }

        e.Handled = true;
        await ViewModel.DeleteLessonCommand.ExecuteAsync(lesson);
    }
}
