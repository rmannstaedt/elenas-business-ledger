using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.UseCases.Economy;
using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Tests.App.ViewModels;

public sealed class EconomyOverviewViewModelTests
{
    [Fact]
    public async Task LoadAsyncUsesCurrentYearAndNavigationReloadsAdjacentYears()
    {
        var service = new FakeEconomyOverviewService();
        var viewModel = CreateViewModel(service: service);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        await viewModel.PreviousYearCommand.ExecuteAsync();
        await viewModel.NextYearCommand.ExecuteAsync();

        Assert.Equal([2026, 2025, 2026], service.RequestedYears);
        Assert.Equal(2026, viewModel.Year);
        Assert.Equal("2026", viewModel.YearText);
        Assert.Single(viewModel.Invoices);
        Assert.Equal("001", viewModel.Invoices[0].InvoiceNumberText);
        Assert.Equal("Invoice 001.pdf", viewModel.Invoices[0].PdfFileName);
        Assert.Equal("1.000,00 DKK", viewModel.Invoices[0].NetText);
        Assert.Equal(12, viewModel.Months.Count);
        Assert.Equal(4, viewModel.Quarters.Count);
        Assert.Equal("1.250,00 DKK", viewModel.YearToDateTotalText);
    }

    [Fact]
    public async Task LoadAsyncPassesCancellationTokenToService()
    {
        var service = new FakeEconomyOverviewService();
        var viewModel = CreateViewModel(service: service);
        using var cancellationTokenSource = new CancellationTokenSource();

        await viewModel.LoadAsync(cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, service.RequestedCancellationTokens.Single());
    }

    [Fact]
    public async Task ExportCsvWritesOnlyAfterUserSelectsDestination()
    {
        var dialog = new FakeEconomyOverviewExportDialog
        {
            Paths = new Queue<string?>([null, @"C:\Exports\economy.csv"])
        };
        var fileService = new FakeEconomyOverviewCsvFileService();
        var viewModel = CreateViewModel(dialog: dialog, fileService: fileService);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        await viewModel.ExportCsvCommand.ExecuteAsync();
        await viewModel.ExportCsvCommand.ExecuteAsync();

        Assert.Equal([2026, 2026], dialog.RequestedYears);
        Assert.Single(fileService.SavedPaths);
        Assert.Equal(@"C:\Exports\economy.csv", fileService.SavedPaths[0]);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task OpenInvoicePdfCommandUsesStoredPdfPath()
    {
        var pdfLauncher = new FakeInvoicePdfLauncher();
        var viewModel = CreateViewModel(pdfLauncher: pdfLauncher);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        await viewModel.OpenInvoicePdfCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.Equal(@"C:\Invoices\Invoice 001.pdf", pdfLauncher.LastOpenedPdfFilePath);
    }

    private static EconomyOverviewViewModel CreateViewModel(
        FakeEconomyOverviewService? service = null,
        FakeEconomyOverviewExportDialog? dialog = null,
        FakeEconomyOverviewCsvFileService? fileService = null,
        FakeInvoicePdfLauncher? pdfLauncher = null) =>
        new(
            service ?? new FakeEconomyOverviewService(),
            dialog ?? new FakeEconomyOverviewExportDialog { Paths = new Queue<string?>([@"C:\Exports\economy.csv"]) },
            fileService ?? new FakeEconomyOverviewCsvFileService(),
            pdfLauncher ?? new FakeInvoicePdfLauncher(),
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
                    ? new EconomyOverviewMonthTotal(month, true, MoneyAmount.Dkk(1000m), MoneyAmount.Dkk(250m), MoneyAmount.Dkk(1250m))
                    : new EconomyOverviewMonthTotal(month, false, null, null, null))
                .ToArray(),
            Enumerable.Range(1, 4)
                .Select(quarter => quarter == 2
                    ? new EconomyOverviewQuarterTotal(quarter, true, MoneyAmount.Dkk(1000m), MoneyAmount.Dkk(250m), MoneyAmount.Dkk(1250m))
                    : new EconomyOverviewQuarterTotal(quarter, false, null, null, null))
                .ToArray(),
            new EconomyOverviewYearToDateTotal(
                MoneyAmount.Dkk(1000m),
                MoneyAmount.Dkk(250m),
                MoneyAmount.Dkk(1250m)));

    private sealed class FakeEconomyOverviewService : IEconomyOverviewService
    {
        public List<int> RequestedYears { get; } = [];

        public List<CancellationToken> RequestedCancellationTokens { get; } = [];

        public Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken)
        {
            RequestedYears.Add(year);
            RequestedCancellationTokens.Add(cancellationToken);
            return Task.FromResult(OperationResult<EconomyOverview>.Success(CreateOverview(year)));
        }
    }

    private sealed class FakeEconomyOverviewExportDialog : IEconomyOverviewExportDialog
    {
        public Queue<string?> Paths { get; init; } = [];

        public List<int> RequestedYears { get; } = [];

        public string? ShowSaveCsvDialog(int year)
        {
            RequestedYears.Add(year);
            return Paths.Count == 0 ? null : Paths.Dequeue();
        }
    }

    private sealed class FakeEconomyOverviewCsvFileService : IEconomyOverviewCsvFileService
    {
        public List<string> SavedPaths { get; } = [];

        public Task<OperationResult<string>> SaveAsync(
            EconomyOverview overview,
            string filePath,
            CancellationToken cancellationToken)
        {
            SavedPaths.Add(filePath);
            return Task.FromResult(OperationResult<string>.Success(filePath));
        }
    }

    private sealed class FakeInvoicePdfLauncher : IInvoicePdfLauncher
    {
        public string? LastOpenedPdfFilePath { get; private set; }

        public Task OpenAsync(string pdfFilePath, CancellationToken cancellationToken)
        {
            LastOpenedPdfFilePath = pdfFilePath;
            return Task.CompletedTask;
        }
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
