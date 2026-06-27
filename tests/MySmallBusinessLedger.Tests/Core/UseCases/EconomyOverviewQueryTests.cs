using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.UseCases.Economy;
using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Tests.Core.UseCases;

public sealed class EconomyOverviewQueryTests
{
    [Fact]
    public async Task LoadAsyncAggregatesInvoicesByPeriodStartAndIncludesBlankPeriods()
    {
        var repository = new FakeInvoiceRepository
        {
            OverviewRowsByYear =
            {
                [2026] =
                [
                    new EconomyInvoiceRecord(
                        new InvoiceNumber(2),
                        new DateOnly(2027, 1, 5),
                        new InvoicePeriod(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31)),
                        @"C:\Invoices\Invoice 002 December.pdf",
                        new InvoiceTotals(MoneyAmount.Dkk(1000m), MoneyAmount.Dkk(250m), MoneyAmount.Dkk(1250m))),
                    new EconomyInvoiceRecord(
                        new InvoiceNumber(1),
                        new DateOnly(2026, 4, 30),
                        new InvoicePeriod(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30)),
                        @"C:\Invoices\Invoice 001 April.pdf",
                        new InvoiceTotals(MoneyAmount.Dkk(300m), MoneyAmount.Dkk(75m), MoneyAmount.Dkk(375m)))
                ],
                [2025] =
                [
                    new EconomyInvoiceRecord(
                        new InvoiceNumber(9),
                        new DateOnly(2025, 6, 30),
                        new InvoicePeriod(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30)),
                        @"C:\Invoices\Invoice 009 June.pdf",
                        new InvoiceTotals(MoneyAmount.Dkk(2000m), MoneyAmount.Dkk(500m), MoneyAmount.Dkk(2500m)))
                ]
            }
        };
        var query = new EconomyOverviewQuery(repository);

        var overview = await query.LoadAsync(2026, CancellationToken.None);

        Assert.Equal([2026, 2025, 2024, 2023, 2022], repository.RequestedYears);
        Assert.Equal(2026, overview.Year);
        Assert.Equal([1, 2], overview.Invoices.Select(invoice => invoice.InvoiceNumber.Value).ToArray());
        Assert.Equal("Invoice 002 December.pdf", overview.Invoices[1].PdfFileName);
        Assert.Equal(@"C:\Invoices\Invoice 002 December.pdf", overview.Invoices[1].PdfFilePath);
        Assert.Equal(MoneyAmount.Dkk(1000m), overview.Invoices[1].Net);
        Assert.Equal(MoneyAmount.Dkk(250m), overview.Invoices[1].Vat);
        Assert.Equal(MoneyAmount.Dkk(1250m), overview.Invoices[1].Total);

        Assert.Equal(12, overview.Months.Count);
        var january = Assert.Single(overview.Months, month => month.Month == 1);
        Assert.False(january.HasInvoices);
        Assert.Null(january.Net);
        Assert.Null(january.Vat);
        Assert.Null(january.Total);

        var april = Assert.Single(overview.Months, month => month.Month == 4);
        Assert.True(april.HasInvoices);
        Assert.Equal(MoneyAmount.Dkk(300m), april.Net);
        Assert.Equal(MoneyAmount.Dkk(75m), april.Vat);
        Assert.Equal(MoneyAmount.Dkk(375m), april.Total);

        var december = Assert.Single(overview.Months, month => month.Month == 12);
        Assert.True(december.HasInvoices);
        Assert.Equal(MoneyAmount.Dkk(1000m), december.Net);
        Assert.Equal(MoneyAmount.Dkk(250m), december.Vat);
        Assert.Equal(MoneyAmount.Dkk(1250m), december.Total);

        Assert.Equal(4, overview.Quarters.Count);
        var q2 = Assert.Single(overview.Quarters, quarter => quarter.Quarter == 2);
        Assert.True(q2.HasInvoices);
        Assert.Equal(MoneyAmount.Dkk(300m), q2.Net);
        Assert.Equal(MoneyAmount.Dkk(75m), q2.Vat);
        Assert.Equal(MoneyAmount.Dkk(375m), q2.Total);

        var q4 = Assert.Single(overview.Quarters, quarter => quarter.Quarter == 4);
        Assert.True(q4.HasInvoices);
        Assert.Equal(MoneyAmount.Dkk(1000m), q4.Net);
        Assert.Equal(MoneyAmount.Dkk(250m), q4.Vat);
        Assert.Equal(MoneyAmount.Dkk(1250m), q4.Total);

        Assert.Equal(MoneyAmount.Dkk(1300m), overview.YearToDate.Net);
        Assert.Equal(MoneyAmount.Dkk(325m), overview.YearToDate.Vat);
        Assert.Equal(MoneyAmount.Dkk(1625m), overview.YearToDate.Total);

        Assert.Equal(5, overview.Years.Count);
        Assert.Equal([2026, 2025, 2024, 2023, 2022], overview.Years.Select(year => year.Year).ToArray());
        var currentYear = Assert.Single(overview.Years, year => year.Year == 2026);
        Assert.True(currentYear.HasInvoices);
        Assert.Equal(MoneyAmount.Dkk(1300m), currentYear.Net);
        Assert.Equal(MoneyAmount.Dkk(325m), currentYear.Vat);
        Assert.Equal(MoneyAmount.Dkk(1625m), currentYear.Total);

        var previousYear = Assert.Single(overview.Years, year => year.Year == 2025);
        Assert.True(previousYear.HasInvoices);
        Assert.Equal(MoneyAmount.Dkk(2000m), previousYear.Net);
        Assert.Equal(MoneyAmount.Dkk(500m), previousYear.Vat);
        Assert.Equal(MoneyAmount.Dkk(2500m), previousYear.Total);

        var emptyYear = Assert.Single(overview.Years, year => year.Year == 2024);
        Assert.False(emptyYear.HasInvoices);
        Assert.Null(emptyYear.Net);
        Assert.Null(emptyYear.Vat);
        Assert.Null(emptyYear.Total);
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        public Dictionary<int, IReadOnlyList<EconomyInvoiceRecord>> OverviewRowsByYear { get; } = [];

        public List<int> RequestedYears { get; } = [];

        public Task<IReadOnlyList<EconomyInvoiceRecord>> ListIssuedInvoiceOverviewRowsAsync(
            int periodYear,
            CancellationToken cancellationToken)
        {
            RequestedYears.Add(periodYear);
            return Task.FromResult(OverviewRowsByYear.TryGetValue(periodYear, out var rows) ? rows : []);
        }

        public Task<IReadOnlyList<InvoiceLessonStatus>> ListLessonStatusesAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<IssuedInvoiceSummary>> ListInvoicesForStudentAsync(
            string studentShortName,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> HasInvoiceForStudentInMonthAsync(
            string studentShortName,
            DateOnly month,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<InvoiceNumber> GetNextInvoiceNumberAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<OperationResult<IssuedInvoice>> ReserveInvoiceAsync(
            InvoiceDraft draft,
            InvoiceTotals totals,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<OperationResult<IssuedInvoice>> FinalizeIssuedInvoiceWithLessonLocksAsync(
            InvoiceNumber invoiceNumber,
            string pdfFilePath,
            IReadOnlyList<int> lessonIds,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<OperationResult<IssuedInvoice>> MarkInvoicePdfPublishedAsync(
            InvoiceNumber invoiceNumber,
            string pdfFilePath,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task ReleaseInvoiceReservationAsync(
            InvoiceNumber invoiceNumber,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<OperationResult<IssuedInvoice>> UpdatePrintStatusAsync(
            InvoiceNumber invoiceNumber,
            InvoicePrintStatus printStatus,
            DateTimeOffset? printedAt,
            string? lastPrintError,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IssuedInvoice?> GetIssuedInvoiceAsync(
            InvoiceNumber invoiceNumber,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IssuedInvoice?> GetIssuingInvoiceAsync(
            InvoiceNumber invoiceNumber,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PaymentStatus> GetPaymentStatusAsync(
            InvoiceNumber invoiceNumber,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task SavePaymentStatusAsync(
            InvoiceNumber invoiceNumber,
            PaymentStatus paymentStatus,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
