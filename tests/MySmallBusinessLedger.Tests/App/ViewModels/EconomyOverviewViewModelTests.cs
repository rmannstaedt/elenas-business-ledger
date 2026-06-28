using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Ports;
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
        Assert.Equal(5, viewModel.Years.Count);
        Assert.Equal(["2026", "2025", "2024", "2023", "2022"], viewModel.Years.Select(year => year.Label).ToArray());
        Assert.Equal("1.250,00 DKK", viewModel.Years[0].TotalText);
        Assert.Equal(string.Empty, viewModel.Years[4].TotalText);
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

    [Fact]
    public async Task AdminModeAllowsSavingManualLegacyMonthOverrides()
    {
        var service = new FakeEconomyOverviewService
        {
            FirstApplicationInvoiceMonth = new DateOnly(2026, 7, 1)
        };
        var viewModel = CreateViewModel(service: service, isAdminMode: true);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.ShowAdminControls);
        Assert.True(viewModel.Months.Single(month => month.Month == 5).CanEditManualTotals);
        Assert.False(viewModel.Months.Single(month => month.Month == 8).CanEditManualTotals);

        var may = viewModel.Months.Single(month => month.Month == 5);
        may.NetText = "2.000,00";
        may.VatText = "500,00";

        await viewModel.SaveManualMonthsCommand.ExecuteAsync();

        Assert.NotNull(service.SavedSettings);
        Assert.Equal(new DateOnly(2026, 7, 1), service.SavedSettings.FirstApplicationInvoiceMonth);
        var savedMay = Assert.Single(service.SavedManualOverrides, manual => manual.Year == 2026 && manual.Month == 5);
        Assert.Equal(MoneyAmount.Dkk(2000m), savedMay.Net);
        Assert.Equal(MoneyAmount.Dkk(500m), savedMay.Vat);
        Assert.Contains(service.SavedManualOverrides, manual =>
            manual.Year == 2026 &&
            manual.Month == 6 &&
            manual.Net == MoneyAmount.Dkk(0m) &&
            manual.Vat == MoneyAmount.Dkk(0m));
        Assert.Equal("Manual month totals saved.", viewModel.ManualEditStatusText);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task AdminModeSaveSkipsReadOnlyIssuedInvoiceLegacyMonths()
    {
        var service = new FakeEconomyOverviewService
        {
            NextLoadResult = OperationResult<EconomyOverview>.Success(CreateOverviewWithIssuedLegacyMonth(2026))
        };
        var viewModel = CreateViewModel(service: service, isAdminMode: true);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        var march = viewModel.Months.Single(month => month.Month == 3);
        var may = viewModel.Months.Single(month => month.Month == 5);
        Assert.True(march.CanEditManualTotals);
        Assert.False(may.CanEditManualTotals);

        march.NetText = "1.212,00";
        march.VatText = "0,00";

        await viewModel.SaveManualMonthsCommand.ExecuteAsync();

        Assert.Contains(service.SavedManualOverrides, manual =>
            manual.Year == 2026 &&
            manual.Month == 3 &&
            manual.Net == MoneyAmount.Dkk(1212m) &&
            manual.Vat == MoneyAmount.Dkk(0m));
        Assert.DoesNotContain(service.SavedManualOverrides, manual => manual.Year == 2026 && manual.Month == 5);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task AdminModeWithoutCutOverKeepsMonthsReadOnly()
    {
        var viewModel = CreateViewModel(isAdminMode: true);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.All(viewModel.Months, month => Assert.False(month.CanEditManualTotals));
        Assert.Contains("Application Settings", viewModel.ManualEditStatusText);
    }

    [Fact]
    public async Task NonAdminModeKeepsLegacyMonthsReadOnly()
    {
        var service = new FakeEconomyOverviewService
        {
            FirstApplicationInvoiceMonth = new DateOnly(2026, 7, 1)
        };
        var viewModel = CreateViewModel(service: service, isAdminMode: false);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.ShowAdminControls);
        Assert.All(viewModel.Months, month => Assert.False(month.CanEditManualTotals));
        Assert.False(viewModel.SaveManualMonthsCommand.CanExecute(null));
    }

    [Fact]
    public void AdminModeKeepsApplicationIssuedLegacyMonthsReadOnly()
    {
        var month = new EconomyOverviewViewModel.EconomyOverviewMonthDisplayModel(
            2026,
            5,
            "maj",
            "1.000,00 DKK",
            "250,00 DKK",
            "1.250,00 DKK",
            EconomyOverviewMonthSource.IssuedInvoices,
            isAdminMode: true,
            firstApplicationInvoiceMonth: new DateOnly(2026, 7, 1));

        Assert.False(month.CanEditManualTotals);
    }

    [Fact]
    public async Task LoadAsyncDisplaysReturnedOverviewDisablesExportButKeepsManualCorrectionActionsAvailable()
    {
        var overview = CreateOverview(2026, new DateOnly(2026, 6, 1));
        var service = new FakeEconomyOverviewService
        {
            NextLoadResult = OperationResult<EconomyOverview>.Failure(
                overview,
                new ValidationError(
                    "EconomyOverview.ManualOverrideIssuedInvoiceConflict",
                    "Manual month totals for 2026-05 conflict with Invoice 001."))
        };
        var viewModel = CreateViewModel(service: service, isAdminMode: true);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.HasError);
        Assert.Contains("Invoice 001", viewModel.ErrorMessage);
        Assert.Single(viewModel.Invoices);
        Assert.Equal(12, viewModel.Months.Count);
        Assert.Equal(4, viewModel.Quarters.Count);
        Assert.Equal(5, viewModel.Years.Count);
        Assert.False(viewModel.ExportCsvCommand.CanExecute(null));
        Assert.True(viewModel.SaveManualMonthsCommand.CanExecute(null));
        Assert.True(viewModel.CancelManualMonthsCommand.CanExecute(null));
    }

    private static EconomyOverviewViewModel CreateViewModel(
        FakeEconomyOverviewService? service = null,
        FakeEconomyOverviewExportDialog? dialog = null,
        FakeEconomyOverviewCsvFileService? fileService = null,
        FakeInvoicePdfLauncher? pdfLauncher = null,
        bool isAdminMode = false) =>
        new(
            service ?? new FakeEconomyOverviewService(),
            dialog ?? new FakeEconomyOverviewExportDialog { Paths = new Queue<string?>([@"C:\Exports\economy.csv"]) },
            fileService ?? new FakeEconomyOverviewCsvFileService(),
            pdfLauncher ?? new FakeInvoicePdfLauncher(),
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
                .Select(month =>
                {
                    var isLegacyManualMonth = firstApplicationInvoiceMonth is not null &&
                        new DateOnly(year, month, 1) < firstApplicationInvoiceMonth.Value;
                    return month == 5
                        ? new EconomyOverviewMonthTotal(
                            month,
                            HasInvoices: !isLegacyManualMonth,
                            MoneyAmount.Dkk(1000m),
                            MoneyAmount.Dkk(250m),
                            MoneyAmount.Dkk(1250m),
                            isLegacyManualMonth ? EconomyOverviewMonthSource.ManualOverride : EconomyOverviewMonthSource.IssuedInvoices,
                            isLegacyManualMonth)
                        : new EconomyOverviewMonthTotal(
                            month,
                            HasInvoices: false,
                            null,
                            null,
                            null,
                            EconomyOverviewMonthSource.None,
                            isLegacyManualMonth);
                })
                .ToArray(),
            Enumerable.Range(1, 4)
                .Select(quarter => quarter == 2
                    ? new EconomyOverviewQuarterTotal(quarter, true, MoneyAmount.Dkk(1000m), MoneyAmount.Dkk(250m), MoneyAmount.Dkk(1250m))
                    : new EconomyOverviewQuarterTotal(quarter, false, null, null, null))
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

    private static EconomyOverview CreateOverviewWithIssuedLegacyMonth(int year) =>
        new(
            year,
            [
                new EconomyOverviewInvoiceRow(
                    new InvoiceNumber(1),
                    new DateOnly(year, 5, 31),
                    new InvoicePeriod(new DateOnly(year, 5, 1), new DateOnly(year, 5, 31)),
                    @"C:\Invoices\Invoice 001.pdf",
                    "Invoice 001.pdf",
                    MoneyAmount.Dkk(2550m),
                    MoneyAmount.Dkk(0m),
                    MoneyAmount.Dkk(2550m))
            ],
            Enumerable.Range(1, 12)
                .Select(month => month == 5
                    ? new EconomyOverviewMonthTotal(
                        month,
                        HasInvoices: true,
                        MoneyAmount.Dkk(2550m),
                        MoneyAmount.Dkk(0m),
                        MoneyAmount.Dkk(2550m),
                        EconomyOverviewMonthSource.IssuedInvoices,
                        IsLegacyManualMonth: true)
                    : new EconomyOverviewMonthTotal(
                        month,
                        HasInvoices: false,
                        null,
                        null,
                        null,
                        EconomyOverviewMonthSource.None,
                        IsLegacyManualMonth: month < 6))
                .ToArray(),
            [
                new EconomyOverviewQuarterTotal(1, HasInvoices: false, null, null, null),
                new EconomyOverviewQuarterTotal(2, HasInvoices: true, MoneyAmount.Dkk(2550m), MoneyAmount.Dkk(0m), MoneyAmount.Dkk(2550m)),
                new EconomyOverviewQuarterTotal(3, HasInvoices: false, null, null, null),
                new EconomyOverviewQuarterTotal(4, HasInvoices: false, null, null, null)
            ],
            new EconomyOverviewYearToDateTotal(
                MoneyAmount.Dkk(2550m),
                MoneyAmount.Dkk(0m),
                MoneyAmount.Dkk(2550m)),
            Enumerable.Range(0, 5)
                .Select(offset => offset == 0
                    ? new EconomyOverviewYearTotal(
                        year,
                        HasInvoices: true,
                        MoneyAmount.Dkk(2550m),
                        MoneyAmount.Dkk(0m),
                        MoneyAmount.Dkk(2550m))
                    : new EconomyOverviewYearTotal(year - offset, HasInvoices: false, null, null, null))
                .ToArray(),
            new DateOnly(year, 6, 1));

    private sealed class FakeEconomyOverviewService : IEconomyOverviewService
    {
        public List<int> RequestedYears { get; } = [];

        public List<CancellationToken> RequestedCancellationTokens { get; } = [];

        public List<EconomyManualMonthOverride> SavedManualOverrides { get; } = [];

        public DateOnly? FirstApplicationInvoiceMonth { get; init; }

        public EconomySettings? SavedSettings { get; private set; }

        public OperationResult<EconomyOverview>? NextLoadResult { get; init; }

        public Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken)
        {
            RequestedYears.Add(year);
            RequestedCancellationTokens.Add(cancellationToken);
            if (NextLoadResult is not null)
            {
                return Task.FromResult(NextLoadResult);
            }

            return Task.FromResult(OperationResult<EconomyOverview>.Success(CreateOverview(year, FirstApplicationInvoiceMonth)));
        }

        public Task<OperationResult<EconomyOverview>> SaveManualMonthOverridesAsync(
            int overviewYear,
            EconomySettings settings,
            IReadOnlyList<EconomyManualMonthOverride> overrides,
            CancellationToken cancellationToken)
        {
            SavedSettings = settings;
            SavedManualOverrides.Clear();
            SavedManualOverrides.AddRange(overrides);
            return Task.FromResult(OperationResult<EconomyOverview>.Success(CreateOverview(overviewYear, settings.FirstApplicationInvoiceMonth) with
            {
                FirstApplicationInvoiceMonth = settings.FirstApplicationInvoiceMonth
            }));
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
