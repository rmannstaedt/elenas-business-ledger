using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.UseCases.Economy;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfDataGridLengthUnitType = System.Windows.Controls.DataGridLengthUnitType;
using WpfPath = System.Windows.Shapes.Path;
using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Tests.App.Views;

public sealed class EconomyOverviewViewTests
{
    [Fact]
    public void ToolbarUsesQuietArrowIconsAndRightAlignedExport()
    {
        RunOnStaThread(() =>
        {
            var view = new EconomyOverviewView(CreateViewModel());
            var buttons = FindChildren<Button>(view);

            var previousYearButton = Assert.Single(buttons, button =>
                AutomationProperties.GetName(button) == "Previous year");
            var nextYearButton = Assert.Single(buttons, button =>
                AutomationProperties.GetName(button) == "Next year");
            Assert.IsType<WpfPath>(previousYearButton.Content);
            Assert.IsType<WpfPath>(nextYearButton.Content);
            Assert.Equal(Brushes.Transparent, previousYearButton.Background);
            Assert.Equal(Brushes.Transparent, nextYearButton.Background);

            var exportButton = Assert.Single(buttons, button => button.Content?.ToString() == "Export CSV");
            Assert.Equal(2, Grid.GetColumn(exportButton));
            Assert.Equal(HorizontalAlignment.Right, exportButton.HorizontalAlignment);
        });
    }

    [Fact]
    public void EconomyTablesDoNotExposeResizableOrDoubleBorderChrome()
    {
        RunOnStaThread(() =>
        {
            var view = new EconomyOverviewView(CreateViewModel());

            var gridsByFirstHeader = FindChildren<WpfDataGrid>(view)
                .Where(grid => grid.Columns.Count > 0)
                .ToDictionary(
                    grid => Convert.ToString(grid.Columns[0].Header, CultureInfo.InvariantCulture) ?? string.Empty);

            Assert.Equal(["Invoice", "Month", "Quarter", "Year"], gridsByFirstHeader.Keys.Order().ToArray());
            Assert.All(gridsByFirstHeader.Values, grid =>
            {
                Assert.False(grid.CanUserResizeColumns);
                Assert.False(grid.CanUserResizeRows);
                Assert.False(grid.CanUserReorderColumns);
                Assert.Equal(new Thickness(1), grid.BorderThickness);
                Assert.Equal(Color.FromRgb(32, 32, 32), Assert.IsType<SolidColorBrush>(grid.BorderBrush).Color);
                Assert.Equal(Color.FromRgb(200, 200, 200), Assert.IsType<SolidColorBrush>(grid.HorizontalGridLinesBrush).Color);
                Assert.Equal(Color.FromRgb(200, 200, 200), Assert.IsType<SolidColorBrush>(grid.VerticalGridLinesBrush).Color);
            });

            var invoiceGrid = gridsByFirstHeader["Invoice"];
            var filenameColumn = invoiceGrid.Columns.Single(column =>
                Convert.ToString(column.Header, CultureInfo.InvariantCulture) == "Filename");
            Assert.Equal(WpfDataGridLengthUnitType.Star, filenameColumn.Width.UnitType);

            foreach (var summaryGrid in new[] { gridsByFirstHeader["Month"], gridsByFirstHeader["Quarter"], gridsByFirstHeader["Year"] })
            {
                Assert.Equal(WpfDataGridLengthUnitType.Pixel, summaryGrid.Columns[0].Width.UnitType);
                Assert.Equal(80, summaryGrid.Columns[0].Width.Value);
                Assert.Equal(ScrollBarVisibility.Disabled, ScrollViewer.GetHorizontalScrollBarVisibility(summaryGrid));
                Assert.Equal(ScrollBarVisibility.Disabled, ScrollViewer.GetVerticalScrollBarVisibility(summaryGrid));
            }

            Assert.Equal(ScrollBarVisibility.Auto, ScrollViewer.GetVerticalScrollBarVisibility(invoiceGrid));

            Assert.Equal(1040, view.Width);
            Assert.Equal(920, view.MinWidth);
        });
    }

    [Fact]
    public void AdminModeShowsManualControlsAndEditableMonthColumns()
    {
        RunOnStaThread(() =>
        {
            var normalView = new EconomyOverviewView(CreateViewModel());
            ProcessDataBindings(normalView);
            Assert.Empty(FindChildren<DatePicker>(normalView));
            var normalSaveButton = Assert.Single(FindChildren<Button>(normalView), button =>
                button.Content?.ToString() == "Save manual totals");
            Assert.Equal(Visibility.Collapsed, Assert.IsAssignableFrom<FrameworkElement>(normalSaveButton.Parent).Visibility);

            var adminView = new EconomyOverviewView(CreateViewModel(isAdminMode: true));
            ProcessDataBindings(adminView);
            Assert.Empty(FindChildren<DatePicker>(adminView));
            var adminSaveButton = Assert.Single(FindChildren<Button>(adminView), button =>
                button.Content?.ToString() == "Save manual totals");
            Assert.Equal(Visibility.Visible, Assert.IsAssignableFrom<FrameworkElement>(adminSaveButton.Parent).Visibility);

            var monthsGrid = FindChildren<WpfDataGrid>(adminView)
                .Single(grid => grid.Columns.Count > 0 &&
                    Convert.ToString(grid.Columns[0].Header, CultureInfo.InvariantCulture) == "Month");
            Assert.False(monthsGrid.IsReadOnly);
            Assert.IsType<DataGridTemplateColumn>(monthsGrid.Columns[1]);
            Assert.IsType<DataGridTemplateColumn>(monthsGrid.Columns[2]);
            Assert.True(monthsGrid.Columns[3].IsReadOnly);
        });
    }

    [Fact]
    public void CloseButtonDoesNotCloseWindowOnEscapeInAdminMode()
    {
        RunOnStaThread(() =>
        {
            var normalView = new EconomyOverviewView(CreateViewModel());
            ProcessDataBindings(normalView);
            var normalCloseButton = Assert.Single(FindChildren<Button>(normalView), button =>
                button.Content?.ToString() == "Close");
            Assert.True(normalCloseButton.IsCancel);

            var adminView = new EconomyOverviewView(CreateViewModel(isAdminMode: true));
            ProcessDataBindings(adminView);
            var adminCloseButton = Assert.Single(FindChildren<Button>(adminView), button =>
                button.Content?.ToString() == "Close");
            Assert.False(adminCloseButton.IsCancel);
        });
    }

    [Fact]
    public void CloseButtonStillClosesWindowInAdminMode()
    {
        RunOnStaThread(() =>
        {
            var storePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            WindowPlacementStore.StorePathOverride = storePath;
            try
            {
                var adminView = new EconomyOverviewView(CreateViewModel(isAdminMode: true));
                var wasClosed = false;
                adminView.Closed += (_, _) => wasClosed = true;
                adminView.Show();
                ProcessDataBindings(adminView);

                var closeButton = Assert.Single(FindChildren<Button>(adminView), button =>
                    button.Content?.ToString() == "Close");
                closeButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                adminView.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

                Assert.True(wasClosed);
            }
            finally
            {
                WindowPlacementStore.StorePathOverride = null;
                if (File.Exists(storePath))
                {
                    File.Delete(storePath);
                }
            }
        });
    }

    [Fact]
    public void EnterInEditableMonthAmountFocusesSameColumnOnNextEditableRow()
    {
        RunOnStaThread(() =>
        {
            var storePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            WindowPlacementStore.StorePathOverride = storePath;
            EconomyOverviewView? view = null;
            try
            {
                var viewModel = CreateViewModel(
                    isAdminMode: true,
                    loadResult: OperationResult<EconomyOverview>.Success(
                        CreateOverview(2026, new DateOnly(2026, 7, 1))));
                viewModel.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                view = new EconomyOverviewView(viewModel);
                view.Show();
                ProcessDataBindings(view);

                var monthsGrid = FindChildren<WpfDataGrid>(view)
                    .Single(grid => grid.Columns.Count > 0 &&
                        Convert.ToString(grid.Columns[0].Header, CultureInfo.InvariantCulture) == "Month");
                var march = viewModel.Months.Single(month => month.Month == 3);
                var april = viewModel.Months.Single(month => month.Month == 4);
                var marchNetTextBox = FindMonthTextBox(monthsGrid, march, nameof(march.NetText));
                marchNetTextBox.Focus();
                Keyboard.Focus(marchNetTextBox);
                Assert.Same(marchNetTextBox, Keyboard.FocusedElement);

                var keyEventArgs = new KeyEventArgs(
                    Keyboard.PrimaryDevice,
                    PresentationSource.FromVisual(marchNetTextBox)!,
                    0,
                    Key.Enter)
                {
                    RoutedEvent = UIElement.PreviewKeyDownEvent,
                    Source = marchNetTextBox
                };
                marchNetTextBox.RaiseEvent(keyEventArgs);
                view.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
                view.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

                var aprilNetTextBox = FindMonthTextBox(monthsGrid, april, nameof(april.NetText));
                Assert.Same(aprilNetTextBox, Keyboard.FocusedElement);
            }
            finally
            {
                view?.Close();
                WindowPlacementStore.StorePathOverride = null;
                if (File.Exists(storePath))
                {
                    File.Delete(storePath);
                }
            }
        });
    }

    [Fact]
    public void BlockingErrorUsesWrappedStatusAreaAndExpandsWindow()
    {
        RunOnStaThread(() =>
        {
            var normalViewModel = CreateViewModel();
            var normalView = new EconomyOverviewView(normalViewModel);
            ProcessDataBindings(normalView);

            var normalErrorText = Assert.IsType<TextBlock>(normalView.FindName("ErrorStatusText"));
            Assert.Equal(TextWrapping.Wrap, normalErrorText.TextWrapping);
            Assert.Equal(Visibility.Collapsed, normalErrorText.Visibility);
            Assert.Equal(760, normalView.Height);

            var failingViewModel = CreateViewModel(
                loadResult: OperationResult<EconomyOverview>.Failure(
                    CreateOverview(2026),
                    new MySmallBusinessLedger.Core.Common.ValidationError(
                        "EconomyOverview.ManualOverrideIssuedInvoiceConflict",
                        "Manual month totals for 2026-05 conflict with invoice 001. Remove either the manual month total or the application-issued invoice for that month.")));
            var failingView = new EconomyOverviewView(failingViewModel);

            failingViewModel.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            ProcessDataBindings(failingView);

            var failingErrorText = Assert.IsType<TextBlock>(failingView.FindName("ErrorStatusText"));
            Assert.Equal(Visibility.Visible, failingErrorText.Visibility);
            Assert.Equal(TextWrapping.Wrap, failingErrorText.TextWrapping);
            Assert.Equal(800, failingView.Height);
            Assert.Equal(760, failingView.MinHeight);
        });
    }

    private static EconomyOverviewViewModel CreateViewModel(
        bool isAdminMode = false,
        OperationResult<EconomyOverview>? loadResult = null) =>
        new(
            new FakeEconomyOverviewService(loadResult),
            new FakeEconomyOverviewExportDialog(),
            new FakeEconomyOverviewCsvFileService(),
            new FakeInvoicePdfLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero)),
            isAdminMode);

    private static EconomyOverview CreateOverview(int year, DateOnly? firstApplicationInvoiceMonth = null) =>
        new(
            year,
            [
                new EconomyOverviewInvoiceRow(
                    new InvoiceNumber(1),
                    new DateOnly(year, 5, 31),
                    new InvoicePeriod(new DateOnly(year, 5, 1), new DateOnly(year, 5, 31)),
                    @"C:\Invoices\Invoice 001.pdf",
                    "Invoice 001.pdf",
                    MoneyAmount.Dkk(1000m),
                    MoneyAmount.Dkk(250m),
                    MoneyAmount.Dkk(1250m))
            ],
            Enumerable.Range(1, 12)
                .Select(month => month == 5
                    ? new EconomyOverviewMonthTotal(month, HasInvoices: true, MoneyAmount.Dkk(1000m), MoneyAmount.Dkk(250m), MoneyAmount.Dkk(1250m))
                    : new EconomyOverviewMonthTotal(month, HasInvoices: false, null, null, null))
                .ToArray(),
            Enumerable.Range(1, 4)
                .Select(quarter => quarter == 2
                    ? new EconomyOverviewQuarterTotal(quarter, HasInvoices: true, MoneyAmount.Dkk(1000m), MoneyAmount.Dkk(250m), MoneyAmount.Dkk(1250m))
                    : new EconomyOverviewQuarterTotal(quarter, HasInvoices: false, null, null, null))
                .ToArray(),
            new EconomyOverviewYearToDateTotal(
                MoneyAmount.Dkk(1000m),
                MoneyAmount.Dkk(250m),
                MoneyAmount.Dkk(1250m)),
            Enumerable.Range(0, 5)
                .Select(offset => offset == 0
                    ? new EconomyOverviewYearTotal(
                        year - offset,
                        HasInvoices: true,
                        MoneyAmount.Dkk(1000m),
                        MoneyAmount.Dkk(250m),
                        MoneyAmount.Dkk(1250m))
                    : new EconomyOverviewYearTotal(year - offset, HasInvoices: false, null, null, null))
                .ToArray(),
            firstApplicationInvoiceMonth);

    private static IReadOnlyList<T> FindChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        var matches = new List<T>();
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is T match)
            {
                matches.Add(match);
            }

            if (child is DependencyObject dependencyObject)
            {
                matches.AddRange(FindChildren<T>(dependencyObject));
            }
        }

        return matches;
    }

    private static void ProcessDataBindings(Window window)
    {
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
        window.UpdateLayout();
    }

    private static TextBox FindMonthTextBox(
        WpfDataGrid grid,
        EconomyOverviewViewModel.EconomyOverviewMonthDisplayModel month,
        string bindingPath)
    {
        grid.ScrollIntoView(month);
        grid.UpdateLayout();

        var row = FindVisualChildren<DataGridRow>(grid)
            .Single(dataGridRow => ReferenceEquals(dataGridRow.Item, month));
        return FindVisualChildren<TextBox>(row)
            .Single(textBox =>
                ReferenceEquals(textBox.DataContext, month) &&
                textBox.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path == bindingPath);
    }

    private static IReadOnlyList<T> FindVisualChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        var matches = new List<T>();
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                matches.Add(match);
            }

            matches.AddRange(FindVisualChildren<T>(child));
        }

        return matches;
    }

    private static void RunOnStaThread(Action action)
    {
        ExceptionDispatchInfo? capturedException = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                capturedException = ExceptionDispatchInfo.Capture(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        capturedException?.Throw();
    }

    private sealed class FakeEconomyOverviewService(OperationResult<EconomyOverview>? loadResult) : IEconomyOverviewService
    {
        public Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken) =>
            Task.FromResult(loadResult ?? OperationResult<EconomyOverview>.Success(CreateOverview(year)));

        public Task<OperationResult<EconomyOverview>> SaveManualMonthOverridesAsync(
            int overviewYear,
            EconomySettings settings,
            IReadOnlyList<EconomyManualMonthOverride> overrides,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<EconomyOverview>.Success(CreateOverview(overviewYear)));
    }

    private sealed class FakeEconomyOverviewExportDialog : IEconomyOverviewExportDialog
    {
        public string? ShowSaveCsvDialog(int year) => null;
    }

    private sealed class FakeEconomyOverviewCsvFileService : IEconomyOverviewCsvFileService
    {
        public Task<OperationResult<string>> SaveAsync(
            EconomyOverview overview,
            string filePath,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<string>.Success(filePath));
    }

    private sealed class FakeInvoicePdfLauncher : IInvoicePdfLauncher
    {
        public Task OpenAsync(string pdfFilePath, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeUiClock : IUiClock
    {
        public FakeUiClock(DateTimeOffset now)
        {
            Now = now;
        }

        public DateTimeOffset Now { get; }
    }
}
