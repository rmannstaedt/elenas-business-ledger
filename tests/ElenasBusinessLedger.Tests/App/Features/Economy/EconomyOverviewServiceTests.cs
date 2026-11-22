using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.UseCases.Economy;
using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;

namespace ElenasBusinessLedger.Tests.App.Services;

// Checks reporting errors, protection of issued months, and the shape of Economy CSV exports.
public sealed class EconomyOverviewServiceTests
{
    [Fact]
    public async Task LoadAsyncReturnsFailureWhenCoreQueryFails()
    {
        var service = new CoreEconomyOverviewService(new EconomyOverviewQuery(new ThrowingInvoiceRepository()));

        var result = await service.LoadAsync(2026, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "EconomyOverview.LoadFailed");
        Assert.Contains("database unavailable", result.Errors[0].Message);
    }

    [Fact]
    public async Task SaveManualMonthOverridesDoesNotPersistManualTotalsForIssuedInvoiceMonths()
    {
        var invoices = new FakeInvoiceRepository
        {
            OverviewRowsByYear =
            {
                [2026] =
                [
                    new EconomyInvoiceRecord(
                        new InvoiceNumber(1),
                        new DateOnly(2026, 5, 31),
                        new InvoicePeriod(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)),
                        @"C:\Invoices\Invoice 001.pdf",
                        new InvoiceTotals(MoneyAmount.Dkk(2550m), MoneyAmount.Dkk(0m), MoneyAmount.Dkk(2550m)))
                ]
            }
        };
        var economy = new FakeEconomyRepository
        {
            Settings = new EconomySettings(new DateOnly(2026, 6, 1))
        };
        var service = new CoreEconomyOverviewService(new EconomyOverviewQuery(invoices, economy), economy, invoices);

        var result = await service.SaveManualMonthOverridesAsync(
            2026,
            new EconomySettings(new DateOnly(2026, 6, 1)),
            [
                new EconomyManualMonthOverride(2026, 3, MoneyAmount.Dkk(1212m), MoneyAmount.Dkk(0m)),
                new EconomyManualMonthOverride(2026, 5, MoneyAmount.Dkk(2550m), MoneyAmount.Dkk(0m))
            ],
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains(economy.LastSavedOverrides, manual =>
            manual.Year == 2026 &&
            manual.Month == 3 &&
            manual.Net == MoneyAmount.Dkk(1212m));
        Assert.Contains(economy.LastSavedOverrides, manual =>
            manual.Year == 2026 &&
            manual.Month == 5 &&
            manual.Net == MoneyAmount.Dkk(0m) &&
            manual.Vat == MoneyAmount.Dkk(0m));
        Assert.DoesNotContain(economy.ManualOverrides, manual => manual.Year == 2026 && manual.Month == 5);
    }

    [Fact]
    public void CsvFormatterCreatesSectionedMachineReadableExport()
    {
        var formatter = new EconomyOverviewCsvFormatter();
        var overview = new EconomyOverview(
            2026,
            [
                new EconomyOverviewInvoiceRow(
                    new InvoiceNumber(1),
                    new DateOnly(2026, 5, 31),
                    new InvoicePeriod(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)),
                    @"C:\Invoices\Invoice 001, Maj.pdf",
                    "Invoice 001, \"Maj\".pdf",
                    MoneyAmount.Dkk(1234.5m),
                    MoneyAmount.Dkk(308.63m),
                    MoneyAmount.Dkk(1543.13m))
            ],
            Enumerable.Range(1, 12)
                .Select(month => month == 5
                    ? new EconomyOverviewMonthTotal(
                        month,
                        HasInvoices: true,
                        MoneyAmount.Dkk(1234.5m),
                        MoneyAmount.Dkk(308.63m),
                        MoneyAmount.Dkk(1543.13m))
                    : new EconomyOverviewMonthTotal(month, HasInvoices: false, null, null, null))
                .ToArray(),
            Enumerable.Range(1, 4)
                .Select(quarter => quarter == 2
                    ? new EconomyOverviewQuarterTotal(
                        quarter,
                        HasInvoices: true,
                        MoneyAmount.Dkk(1234.5m),
                        MoneyAmount.Dkk(308.63m),
                        MoneyAmount.Dkk(1543.13m))
                    : new EconomyOverviewQuarterTotal(quarter, HasInvoices: false, null, null, null))
                .ToArray(),
            new EconomyOverviewYearToDateTotal(
                MoneyAmount.Dkk(1234.5m),
                MoneyAmount.Dkk(308.63m),
                MoneyAmount.Dkk(1543.13m)),
            [
                new EconomyOverviewYearTotal(
                    2026,
                    HasInvoices: true,
                    MoneyAmount.Dkk(1234.5m),
                    MoneyAmount.Dkk(308.63m),
                    MoneyAmount.Dkk(1543.13m)),
                new EconomyOverviewYearTotal(2025, HasInvoices: false, null, null, null),
                new EconomyOverviewYearTotal(2024, HasInvoices: false, null, null, null),
                new EconomyOverviewYearTotal(2023, HasInvoices: false, null, null, null),
                new EconomyOverviewYearTotal(2022, HasInvoices: false, null, null, null)
            ]);

        var csv = formatter.Format(overview);

        Assert.Equal(
            string.Join(
                Environment.NewLine,
                [
                    "Section,Year,Label,InvoiceNumber,Filename,Net,VAT,Total,Source",
                    "Invoice,2026,,001,\"Invoice 001, \"\"Maj\"\".pdf\",1234.50,308.63,1543.13,IssuedInvoice",
                    "Month,2026,2026-01,,,,,,",
                    "Month,2026,2026-02,,,,,,",
                    "Month,2026,2026-03,,,,,,",
                    "Month,2026,2026-04,,,,,,",
                    "Month,2026,2026-05,,,1234.50,308.63,1543.13,IssuedInvoices",
                    "Month,2026,2026-06,,,,,,",
                    "Month,2026,2026-07,,,,,,",
                    "Month,2026,2026-08,,,,,,",
                    "Month,2026,2026-09,,,,,,",
                    "Month,2026,2026-10,,,,,,",
                    "Month,2026,2026-11,,,,,,",
                    "Month,2026,2026-12,,,,,,",
                    "Quarter,2026,Q1,,,,,,",
                    "Quarter,2026,Q2,,,1234.50,308.63,1543.13,DisplayedTotals",
                    "Quarter,2026,Q3,,,,,,",
                    "Quarter,2026,Q4,,,,,,",
                    "Year,2026,2026,,,1234.50,308.63,1543.13,DisplayedTotals",
                    "Year,2025,2025,,,,,,",
                    "Year,2024,2024,,,,,,",
                    "Year,2023,2023,,,,,,",
                    "Year,2022,2022,,,,,,"
                ]),
            csv);
    }

    [Fact]
    public void CsvFormatterMarksManualMonthOverrideRows()
    {
        var formatter = new EconomyOverviewCsvFormatter();
        var overview = new EconomyOverview(
            2026,
            [],
            Enumerable.Range(1, 12)
                .Select(month => month == 1
                    ? new EconomyOverviewMonthTotal(
                        month,
                        HasInvoices: false,
                        MoneyAmount.Dkk(100m),
                        MoneyAmount.Dkk(25m),
                        MoneyAmount.Dkk(125m),
                        EconomyOverviewMonthSource.ManualOverride,
                        IsLegacyManualMonth: true)
                    : new EconomyOverviewMonthTotal(month, HasInvoices: false, null, null, null))
                .ToArray(),
            [
                new EconomyOverviewQuarterTotal(1, HasInvoices: false, MoneyAmount.Dkk(100m), MoneyAmount.Dkk(25m), MoneyAmount.Dkk(125m)),
                new EconomyOverviewQuarterTotal(2, HasInvoices: false, null, null, null),
                new EconomyOverviewQuarterTotal(3, HasInvoices: false, null, null, null),
                new EconomyOverviewQuarterTotal(4, HasInvoices: false, null, null, null)
            ],
            new EconomyOverviewYearToDateTotal(
                MoneyAmount.Dkk(100m),
                MoneyAmount.Dkk(25m),
                MoneyAmount.Dkk(125m)),
            [
                new EconomyOverviewYearTotal(2026, HasInvoices: false, MoneyAmount.Dkk(100m), MoneyAmount.Dkk(25m), MoneyAmount.Dkk(125m)),
                new EconomyOverviewYearTotal(2025, HasInvoices: false, null, null, null),
                new EconomyOverviewYearTotal(2024, HasInvoices: false, null, null, null),
                new EconomyOverviewYearTotal(2023, HasInvoices: false, null, null, null),
                new EconomyOverviewYearTotal(2022, HasInvoices: false, null, null, null)
            ],
            new DateOnly(2026, 2, 1));

        var csv = formatter.Format(overview);

        Assert.Contains("Month,2026,2026-01,,,100.00,25.00,125.00,ManualOverride", csv);
        Assert.Contains("Quarter,2026,Q1,,,100.00,25.00,125.00,DisplayedTotals", csv);
        Assert.Contains("Year,2026,2026,,,100.00,25.00,125.00,DisplayedTotals", csv);
    }

    // Fails an Economy read so the reporting service's error handling can be tested.
    private sealed class ThrowingInvoiceRepository : IEconomyInvoiceReader
    {
        public Task<IReadOnlyList<EconomyInvoiceRecord>> ListIssuedInvoiceOverviewRowsAsync(
            int periodYear,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("database unavailable");
    }

    // Supplies issued rows by year so manual override tests can identify invoice-backed months.
    private sealed class FakeInvoiceRepository : IEconomyInvoiceReader
    {
        public Dictionary<int, IReadOnlyList<EconomyInvoiceRecord>> OverviewRowsByYear { get; } = [];

        public Task<IReadOnlyList<EconomyInvoiceRecord>> ListIssuedInvoiceOverviewRowsAsync(
            int periodYear,
            CancellationToken cancellationToken) =>
            Task.FromResult(OverviewRowsByYear.TryGetValue(periodYear, out var rows) ? rows : []);
    }

    // Holds settings and captures override writes so service tests can check what would be persisted.
    private sealed class FakeEconomyRepository : IEconomyRepository
    {
        public EconomySettings? Settings { get; set; }

        public List<EconomyManualMonthOverride> ManualOverrides { get; } = [];

        public List<EconomyManualMonthOverride> LastSavedOverrides { get; } = [];

        public Task<EconomySettings?> GetSettingsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Settings);

        public Task<IReadOnlyList<EconomyManualMonthOverride>> ListManualMonthOverridesAsync(
            int fromYearInclusive,
            int toYearInclusive,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EconomyManualMonthOverride>>(
                ManualOverrides
                    .Where(manual => manual.Year >= fromYearInclusive && manual.Year <= toYearInclusive)
                    .ToArray());

        public Task SaveSettingsAsync(EconomySettings settings, CancellationToken cancellationToken)
        {
            Settings = settings;
            return Task.CompletedTask;
        }

        public Task SaveManualMonthOverridesAsync(
            IReadOnlyList<EconomyManualMonthOverride> overrides,
            CancellationToken cancellationToken)
        {
            SaveOverrides(overrides);
            return Task.CompletedTask;
        }

        public Task SaveSettingsAndManualMonthOverridesAsync(
            EconomySettings settings,
            IReadOnlyList<EconomyManualMonthOverride> overrides,
            CancellationToken cancellationToken)
        {
            Settings = settings;
            SaveOverrides(overrides);
            return Task.CompletedTask;
        }

        private void SaveOverrides(IReadOnlyList<EconomyManualMonthOverride> overrides)
        {
            LastSavedOverrides.Clear();
            LastSavedOverrides.AddRange(overrides);
            foreach (var manual in overrides)
            {
                ManualOverrides.RemoveAll(stored => stored.Year == manual.Year && stored.Month == manual.Month);
                if (manual.Net.Amount != 0m || manual.Vat.Amount != 0m)
                {
                    ManualOverrides.Add(manual);
                }
            }
        }
    }
}
