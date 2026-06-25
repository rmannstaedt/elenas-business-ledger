using System.Collections.ObjectModel;
using System.Globalization;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Ports;
using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;
using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

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
                SELECT IssuedInvoiceLines.*
                FROM IssuedInvoiceLines
                INNER JOIN IssuedInvoices ON IssuedInvoices.Id = IssuedInvoiceLines.IssuedInvoiceEntityId
                WHERE IssuedInvoices.IssueStatus = 'Issued'
                  AND LessonDateTime >= {fromKey}
                  AND LessonDateTime < {toKeyExclusive}
                """)
            .AsNoTracking()
            .Include(line => line.Invoice)
            .OrderBy(line => line.LessonDateTime)
            .Select(line => new InvoiceLessonStatus(
                line.Invoice.StudentShortName,
                ParseLessonDateTime(line.LessonDateTime),
                line.Invoice.PaymentStatus,
                new InvoiceNumber(line.Invoice.InvoiceNumber)))
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
            .Where(invoice => invoice.IssueStatus == InvoiceIssueStatus.Issued)
            .OrderBy(invoice => invoice.InvoiceNumber)
            .Select(invoice => new IssuedInvoiceSummary(
                new InvoiceNumber(invoice.InvoiceNumber),
                invoice.StudentShortName,
                invoice.InvoiceDate,
                invoice.PaymentStatus,
                invoice.PdfFilePath,
                invoice.IssueStatus,
                invoice.PrintStatus,
                invoice.PrintedAt,
                invoice.LastPrintError,
                new InvoicePeriod(invoice.PeriodFrom, invoice.PeriodTo)))
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
                    invoice.IssueStatus == InvoiceIssueStatus.Issued &&
                    invoice.StudentShortName == studentShortName &&
                    invoice.PeriodFrom < nextMonth &&
                    invoice.PeriodTo >= firstDay,
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

    public async Task<OperationResult<IssuedInvoice>> ReserveInvoiceAsync(
        InvoiceDraft draft,
        InvoiceTotals totals,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var highest = await context.IssuedInvoices
            .AsNoTracking()
            .Select(invoice => (int?)invoice.InvoiceNumber)
            .MaxAsync(cancellationToken);
        var invoiceNumber = new InvoiceNumber((highest ?? 0) + 1);
        var reservedInvoice = draft.Issue(
            invoiceNumber,
            totals,
            pdfFilePath: null,
            InvoiceIssueStatus.Issuing,
            InvoicePrintStatus.NotPrinted,
            printedAt: null,
            lastPrintError: null);

        var exists = await context.IssuedInvoices.AnyAsync(
            storedInvoice => storedInvoice.InvoiceNumber == invoiceNumber.Value,
            cancellationToken);

        if (exists)
        {
            return OperationResult<IssuedInvoice>.Failure(
                reservedInvoice,
                new ValidationError("Invoice.NumberAlreadyExists", "An issued invoice with this number already exists."));
        }

        context.IssuedInvoices.Add(ToEntity(reservedInvoice));
        await context.SaveChangesAsync(cancellationToken);
        return OperationResult<IssuedInvoice>.Success(reservedInvoice);
    }

    public async Task<OperationResult<IssuedInvoice>> FinalizeIssuedInvoiceAsync(
        InvoiceNumber invoiceNumber,
        string pdfFilePath,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var invoice = await context.IssuedInvoices
            .Include(storedInvoice => storedInvoice.Lines)
            .SingleOrDefaultAsync(
                storedInvoice =>
                    storedInvoice.InvoiceNumber == invoiceNumber.Value &&
                    storedInvoice.IssueStatus == InvoiceIssueStatus.Issuing,
                cancellationToken);

        if (invoice is null)
        {
            return OperationResult<IssuedInvoice>.Failure(
                new ValidationError("Invoice.NotFound", "The reserved invoice could not be found."));
        }

        invoice.PdfFilePath = pdfFilePath;
        invoice.IssueStatus = InvoiceIssueStatus.Issued;
        await context.SaveChangesAsync(cancellationToken);

        return OperationResult<IssuedInvoice>.Success(ToDomain(invoice));
    }

    public async Task ReleaseInvoiceReservationAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var invoice = await context.IssuedInvoices
            .Include(storedInvoice => storedInvoice.Lines)
            .SingleOrDefaultAsync(
                storedInvoice =>
                    storedInvoice.InvoiceNumber == invoiceNumber.Value &&
                    storedInvoice.IssueStatus == InvoiceIssueStatus.Issuing,
                cancellationToken);

        if (invoice is null)
        {
            return;
        }

        context.IssuedInvoices.Remove(invoice);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OperationResult<IssuedInvoice>> UpdatePrintStatusAsync(
        InvoiceNumber invoiceNumber,
        InvoicePrintStatus printStatus,
        DateTimeOffset? printedAt,
        string? lastPrintError,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var invoice = await context.IssuedInvoices
            .Include(storedInvoice => storedInvoice.Lines)
            .SingleOrDefaultAsync(
                storedInvoice =>
                    storedInvoice.InvoiceNumber == invoiceNumber.Value &&
                    storedInvoice.IssueStatus == InvoiceIssueStatus.Issued,
                cancellationToken);

        if (invoice is null)
        {
            return OperationResult<IssuedInvoice>.Failure(
                new ValidationError("Invoice.NotFound", "The issued invoice could not be found."));
        }

        invoice.PrintStatus = printStatus;
        invoice.PrintedAt = printedAt;
        invoice.LastPrintError = lastPrintError;
        await context.SaveChangesAsync(cancellationToken);

        return OperationResult<IssuedInvoice>.Success(ToDomain(invoice));
    }

    public async Task<PaymentStatus> GetPaymentStatusAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        return await context.IssuedInvoices
            .AsNoTracking()
            .Where(invoice =>
                invoice.InvoiceNumber == invoiceNumber.Value &&
                invoice.IssueStatus == InvoiceIssueStatus.Issued)
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
            storedInvoice =>
                storedInvoice.InvoiceNumber == invoiceNumber.Value &&
                storedInvoice.IssueStatus == InvoiceIssueStatus.Issued,
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
            .SingleOrDefaultAsync(
                storedInvoice =>
                    storedInvoice.InvoiceNumber == invoiceNumber.Value &&
                    storedInvoice.IssueStatus == InvoiceIssueStatus.Issued,
                cancellationToken);

        return invoice is null ? null : ToDomain(invoice);
    }

    public async Task<IssuedInvoice?> GetIssuingInvoiceAsync(
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var invoice = await context.IssuedInvoices
            .AsNoTracking()
            .Include(storedInvoice => storedInvoice.Lines)
            .SingleOrDefaultAsync(
                storedInvoice =>
                    storedInvoice.InvoiceNumber == invoiceNumber.Value &&
                    storedInvoice.IssueStatus == InvoiceIssueStatus.Issuing,
                cancellationToken);

        return invoice is null ? null : ToDomain(invoice);
    }

    private static IssuedInvoiceEntity ToEntity(IssuedInvoice invoice)
    {
        var entity = new IssuedInvoiceEntity
        {
            InvoiceNumber = invoice.InvoiceNumber.Value,
            InvoiceDate = invoice.InvoiceDate,
            PeriodFrom = invoice.Period.From,
            PeriodTo = invoice.Period.To,
            CompanyName = invoice.Company.Name,
            CompanyAddress = invoice.Company.Address,
            CompanyCvr = invoice.Company.Cvr,
            CompanyPaymentInstructions = invoice.Company.PaymentInstructions,
            CompanyBankRegistrationNumber = invoice.Company.BankRegistrationNumber,
            CompanyBankAccountNumber = invoice.Company.BankAccountNumber,
            CompanyMobilePayAccount = invoice.Company.MobilePayAccount,
            StudentShortName = invoice.Student.ShortName,
            StudentFullName = invoice.Student.FullName,
            StudentAddress = invoice.Student.Address,
            TotalWithoutVatAmount = invoice.Totals.TotalWithoutVat.Amount,
            VatAmount = invoice.Totals.VatAmount.Amount,
            TotalAmount = invoice.Totals.Total.Amount,
            Currency = invoice.Totals.Total.Currency,
            PdfFilePath = invoice.PdfFilePath ?? string.Empty,
            PaymentStatus = PaymentStatus.Outstanding,
            IssueStatus = invoice.IssueStatus,
            PrintStatus = invoice.PrintStatus,
            PrintedAt = invoice.PrintedAt,
            LastPrintError = invoice.LastPrintError
        };

        foreach (var line in invoice.Lines)
        {
            entity.Lines.Add(new IssuedInvoiceLineEntity
            {
                LessonDateTime = FormatLessonDateTime(line.LessonDateTime),
                PlanName = line.PlanName,
                PlanCatalogId = line.PlanCatalogId,
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
                new MoneyAmount(line.PriceWithoutVatAmount, line.Currency),
                line.PlanCatalogId))
            .ToArray();

        var pdfFilePath = entity.IssueStatus == InvoiceIssueStatus.Issuing &&
            string.IsNullOrWhiteSpace(entity.PdfFilePath)
                ? null
                : entity.PdfFilePath;

        return new IssuedInvoice(
            new InvoiceNumber(entity.InvoiceNumber),
            entity.InvoiceDate,
            new InvoicePeriod(entity.PeriodFrom, entity.PeriodTo),
            new CompanySnapshot(
                entity.CompanyName,
                entity.CompanyAddress,
                entity.CompanyCvr,
                entity.CompanyPaymentInstructions,
                entity.CompanyBankRegistrationNumber,
                entity.CompanyBankAccountNumber,
                entity.CompanyMobilePayAccount),
            new StudentSnapshot(entity.StudentShortName, entity.StudentFullName, entity.StudentAddress),
            new ReadOnlyCollection<IssuedInvoiceLine>(lines),
            new InvoiceTotals(
                new MoneyAmount(entity.TotalWithoutVatAmount, currency),
                new MoneyAmount(entity.VatAmount, currency),
                new MoneyAmount(entity.TotalAmount, currency)),
            pdfFilePath,
            entity.IssueStatus,
            entity.PrintStatus,
            entity.PrintedAt,
            entity.LastPrintError);
    }

    private static string FormatLessonDateTime(DateTimeOffset lessonDateTime) =>
        lessonDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseLessonDateTime(string lessonDateTime) =>
        DateTimeOffset.ParseExact(lessonDateTime, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static string ToDateRangeKey(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
