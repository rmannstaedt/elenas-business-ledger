using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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

    private void MonthManualTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Return) ||
            sender is not TextBox textBox ||
            textBox.DataContext is not EconomyOverviewViewModel.EconomyOverviewMonthDisplayModel currentMonth)
        {
            return;
        }

        var bindingPath = textBox.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path;
        if (bindingPath is not (
                nameof(EconomyOverviewViewModel.EconomyOverviewMonthDisplayModel.NetText) or
                nameof(EconomyOverviewViewModel.EconomyOverviewMonthDisplayModel.VatText)))
        {
            return;
        }

        var grid = FindVisualAncestor<DataGrid>(textBox);
        var nextMonth = grid is null ? null : FindNextEditableMonth(grid, currentMonth);
        if (grid is null || nextMonth is null)
        {
            return;
        }

        e.Handled = true;
        grid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
        grid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);
        grid.SelectedItem = nextMonth;
        SetCurrentCell(grid, nextMonth, bindingPath);
        grid.Dispatcher.BeginInvoke(
            () => FocusMonthTextBox(grid, nextMonth, bindingPath),
            DispatcherPriority.Input);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();

    private static EconomyOverviewViewModel.EconomyOverviewMonthDisplayModel? FindNextEditableMonth(
        DataGrid grid,
        EconomyOverviewViewModel.EconomyOverviewMonthDisplayModel currentMonth)
    {
        var foundCurrent = false;
        foreach (var month in grid.Items.OfType<EconomyOverviewViewModel.EconomyOverviewMonthDisplayModel>())
        {
            if (!foundCurrent)
            {
                foundCurrent = ReferenceEquals(month, currentMonth);
                continue;
            }

            if (month.CanEditManualTotals)
            {
                return month;
            }
        }

        return null;
    }

    private static void SetCurrentCell(
        DataGrid grid,
        EconomyOverviewViewModel.EconomyOverviewMonthDisplayModel month,
        string bindingPath)
    {
        var header = bindingPath == nameof(EconomyOverviewViewModel.EconomyOverviewMonthDisplayModel.NetText)
            ? "Net"
            : "VAT";
        var column = grid.Columns.FirstOrDefault(candidate => string.Equals(
            Convert.ToString(candidate.Header, System.Globalization.CultureInfo.InvariantCulture),
            header,
            StringComparison.Ordinal));
        if (column is not null)
        {
            grid.CurrentCell = new DataGridCellInfo(month, column);
        }
    }

    private static void FocusMonthTextBox(
        DataGrid grid,
        EconomyOverviewViewModel.EconomyOverviewMonthDisplayModel month,
        string bindingPath)
    {
        grid.ScrollIntoView(month);
        grid.UpdateLayout();

        var row = FindVisualChildren<DataGridRow>(grid)
            .FirstOrDefault(dataGridRow => ReferenceEquals(dataGridRow.Item, month));
        var textBox = row is null
            ? null
            : FindVisualChildren<TextBox>(row).FirstOrDefault(candidate =>
                ReferenceEquals(candidate.DataContext, month) &&
                candidate.IsVisible &&
                candidate.IsEnabled &&
                candidate.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path == bindingPath);
        if (textBox is null)
        {
            return;
        }

        textBox.Focus();
        Keyboard.Focus(textBox);
        textBox.SelectAll();
    }

    private static T? FindVisualAncestor<T>(DependencyObject current)
        where T : DependencyObject
    {
        for (var parent = VisualTreeHelper.GetParent(current);
             parent is not null;
             parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is T match)
            {
                return match;
            }
        }

        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
