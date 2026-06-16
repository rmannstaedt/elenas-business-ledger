using System.Collections.ObjectModel;
using System.Globalization;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Ports;
using MoneyAmount = ElenaSmallBusinessLedger.Core.Money.Money;
using Microsoft.EntityFrameworkCore;

namespace ElenaSmallBusinessLedger.Infrastructure.Persistence;

public sealed class SqliteInvoiceRepository : IInvoiceRepository
{
    private readonly Func<LedgerDbContext> createContext;

    public SqliteInvoiceRepository(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    public async Task<IReadOnlyList<InvoiceLessonStatus>> ListLessonStatusesAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var fromKey = ToDateRangeKey(from);
        var toKeyExclusive = ToDateRangeKey(to.AddDays(1));
        var lines = await context.IssuedInvoiceLines
            .FromSqlInterpolated($"""
                SELECT *
                FROM IssuedInvoiceLines
                WHERE LessonDateTime >= {fromKey}
                  AND LessonDateTime < {toKeyExclusive}
                """)
            .AsNoTracking()
            .Include(line => line.Invoice)
            .OrderBy(line => line.LessonDateTime)
            .Select(line => new InvoiceLessonStatus(
                line.Invoice.StudentShortName,
                ParseLessonDateTime(line.LessonDateTime),
                line.Invoice.PaymentStatus))
            .ToArrayAsync(cancellationToken);

        return lines;
    }

    public async Task<IReadOnlyList<IssuedInvoiceSummary>> ListInvoicesForStudentAsync(
        string studentShortName,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        return await context.IssuedInvoices
            .AsNoTracking()
            .Where(invoice => invoice.StudentShortName == studentShortName)
            .OrderBy(invoice => invoice.InvoiceNumber)
            .Select(invoice => new IssuedInvoiceSummary(
                new InvoiceNumber(invoice.InvoiceNumber),
                invoice.StudentShortName,
                invoice.InvoiceDate,
                invoice.PaymentStatus,
                invoice.PdfFilePath))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> HasInvoiceForStudentInMonthAsync(
        string studentShortName,
        DateOnly month,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var firstDay = new DateOnly(month.Year, month.Month, 1);
        var nextMonth = firstDay.AddMonths(1);
        return await context.IssuedInvoices
            .AsNoTracking()
            .AnyAsync(
                invoice =>
                    invoice.StudentShortName == studentShortName &&
                    invoice.InvoiceDate >= firstDay &&
                    invoice.InvoiceDate < nextMonth,
                cancellationToken);
    }

    public async Task<InvoiceNumber> GetNextInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var highest = await context.IssuedInvoices
            .AsNoTracking()
            .Select(invoice => (int?)invoice.InvoiceNumber)
            .MaxAsync(cancellationToken);

        return new InvoiceNumber((highest ?? 0) + 1);
    }

    public async Task<OperationResult<IssuedInvoice>> SaveIssuedInvoiceAsync(
        IssuedInvoice invoice,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var exists = await context.IssuedInvoices.AnyAsync(
            storedInvoice => storedInvoice.InvoiceNumber == invoice.InvoiceNumber.Value,
            cancellationToken);

        if (exists)
        {
            return OperationResult<IssuedInvoice>.Failure(
                invoice,
                new ValidationError("Invoice.NumberAlreadyExists", "An issued invoice with this number already exists."));
        }

        context.IssuedInvoices.Add(ToEntity(invoice));
        await context.SaveChangesAsync(cancellationToken);
        return OperationResult<IssuedInvoice>.Success(invoice);
    }

    public async Task<PaymentStatus> GetPaymentStatusAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        return await context.IssuedInvoices
            .AsNoTracking()
            .Where(invoice => invoice.InvoiceNumber == invoiceNumber.Value)
            .Select(invoice => invoice.PaymentStatus)
            .SingleAsync(cancellationToken);
    }

    public async Task SavePaymentStatusAsync(
        InvoiceNumber invoiceNumber,
        PaymentStatus paymentStatus,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var invoice = await context.IssuedInvoices.SingleAsync(
            storedInvoice => storedInvoice.InvoiceNumber == invoiceNumber.Value,
            cancellationToken);

        invoice.PaymentStatus = paymentStatus;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IssuedInvoice?> GetIssuedInvoiceAsync(
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var invoice = await context.IssuedInvoices
            .AsNoTracking()
            .Include(storedInvoice => storedInvoice.Lines)
            .SingleOrDefaultAsync(storedInvoice => storedInvoice.InvoiceNumber == invoiceNumber.Value, cancellationToken);

        return invoice is null ? null : ToDomain(invoice);
    }

    private static IssuedInvoiceEntity ToEntity(IssuedInvoice invoice)
    {
        var entity = new IssuedInvoiceEntity
        {
            InvoiceNumber = invoice.InvoiceNumber.Value,
            InvoiceDate = invoice.InvoiceDate,
            CompanyName = invoice.Company.Name,
            CompanyAddress = invoice.Company.Address,
            CompanyCvr = invoice.Company.Cvr,
            StudentShortName = invoice.Student.ShortName,
            StudentFullName = invoice.Student.FullName,
            StudentAddress = invoice.Student.Address,
            TotalWithoutVatAmount = invoice.Totals.TotalWithoutVat.Amount,
            VatAmount = invoice.Totals.VatAmount.Amount,
            TotalAmount = invoice.Totals.Total.Amount,
            Currency = invoice.Totals.Total.Currency,
            PdfFilePath = invoice.PdfFilePath,
            PaymentStatus = PaymentStatus.Outstanding
        };

        foreach (var line in invoice.Lines)
        {
            entity.Lines.Add(new IssuedInvoiceLineEntity
            {
                LessonDateTime = FormatLessonDateTime(line.LessonDateTime),
                PlanName = line.PlanName,
                PriceWithoutVatAmount = line.PriceWithoutVat.Amount,
                Currency = line.PriceWithoutVat.Currency
            });
        }

        return entity;
    }

    private static IssuedInvoice ToDomain(IssuedInvoiceEntity entity)
    {
        var currency = entity.Currency;
        var lines = entity.Lines
            .OrderBy(line => line.LessonDateTime)
            .Select(line => new IssuedInvoiceLine(
                ParseLessonDateTime(line.LessonDateTime),
                line.PlanName,
                new MoneyAmount(line.PriceWithoutVatAmount, line.Currency)))
            .ToArray();

        return new IssuedInvoice(
            new InvoiceNumber(entity.InvoiceNumber),
            entity.InvoiceDate,
            new CompanySnapshot(entity.CompanyName, entity.CompanyAddress, entity.CompanyCvr),
            new StudentSnapshot(entity.StudentShortName, entity.StudentFullName, entity.StudentAddress),
            new ReadOnlyCollection<IssuedInvoiceLine>(lines),
            new InvoiceTotals(
                new MoneyAmount(entity.TotalWithoutVatAmount, currency),
                new MoneyAmount(entity.VatAmount, currency),
                new MoneyAmount(entity.TotalAmount, currency)),
            entity.PdfFilePath);
    }

    private static string FormatLessonDateTime(DateTimeOffset lessonDateTime) =>
        lessonDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseLessonDateTime(string lessonDateTime) =>
        DateTimeOffset.ParseExact(lessonDateTime, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static string ToDateRangeKey(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
