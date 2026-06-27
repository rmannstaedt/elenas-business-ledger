using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
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

    private static EconomyOverviewViewModel CreateViewModel() =>
        new(
            new FakeEconomyOverviewService(),
            new FakeEconomyOverviewExportDialog(),
            new FakeEconomyOverviewCsvFileService(),
            new FakeInvoicePdfLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero)));

    private static EconomyOverview CreateOverview(int year) =>
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
                .ToArray());

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

    private sealed class FakeEconomyOverviewService : IEconomyOverviewService
    {
        public Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<EconomyOverview>.Success(CreateOverview(year)));
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
