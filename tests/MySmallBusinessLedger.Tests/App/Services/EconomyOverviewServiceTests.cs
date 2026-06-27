using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.UseCases.Economy;
using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Tests.App.Services;

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
                MoneyAmount.Dkk(1543.13m)));

        var csv = formatter.Format(overview);

        Assert.Equal(
            string.Join(
                Environment.NewLine,
                [
                    "Section,Year,Label,InvoiceNumber,Filename,Net,VAT,Total",
                    "Invoice,2026,,001,\"Invoice 001, \"\"Maj\"\".pdf\",1234.50,308.63,1543.13",
                    "Month,2026,2026-01,,,,,",
                    "Month,2026,2026-02,,,,,",
                    "Month,2026,2026-03,,,,,",
                    "Month,2026,2026-04,,,,,",
                    "Month,2026,2026-05,,,1234.50,308.63,1543.13",
                    "Month,2026,2026-06,,,,,",
                    "Month,2026,2026-07,,,,,",
                    "Month,2026,2026-08,,,,,",
                    "Month,2026,2026-09,,,,,",
                    "Month,2026,2026-10,,,,,",
                    "Month,2026,2026-11,,,,,",
                    "Month,2026,2026-12,,,,,",
                    "Quarter,2026,Q1,,,,,",
                    "Quarter,2026,Q2,,,1234.50,308.63,1543.13",
                    "Quarter,2026,Q3,,,,,",
                    "Quarter,2026,Q4,,,,,",
                    "YearToDate,2026,YTD,,,1234.50,308.63,1543.13"
                ]),
            csv);
    }

    private sealed class ThrowingInvoiceRepository : IInvoiceRepository
    {
        public Task<IReadOnlyList<EconomyInvoiceRecord>> ListIssuedInvoiceOverviewRowsAsync(
            int periodYear,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("database unavailable");

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
