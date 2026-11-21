using System.Collections.ObjectModel;
using System.Globalization;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;
using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;
using Microsoft.EntityFrameworkCore;

namespace ElenasBusinessLedger.Infrastructure.Persistence;

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
        CancellationToken cancellationToken,
        IReadOnlyList<int>? includeLessonIds = null)
    {
        await using var context = createContext();
        var issuedInvoiceLines = await context.IssuedInvoiceLines
            .AsNoTracking()
            .Include(line => line.Invoice)
            .Where(line => line.LessonId > 0)
            .Where(line => line.Invoice.IssueStatus == InvoiceIssueStatus.Issued)
            .Select(line => new
            {
                line.LessonId,
                line.Invoice.InvoiceNumber,
                line.Invoice.PaymentStatus
            })
            .ToArrayAsync(cancellationToken);
        var invoiceLinesByLessonId = issuedInvoiceLines
            .GroupBy(line => line.LessonId)
            .ToDictionary(group => group.Key, group => group.First());
        var invoicedLessonIds = invoiceLinesByLessonId.Keys.ToArray();

        var invoicedLessons = await context.Lessons
            .AsNoTracking()
            .Include(lesson => lesson.Student)
            .Where(lesson => invoicedLessonIds.Contains(lesson.Id))
            .ToArrayAsync(cancellationToken);

        var includedIds = includeLessonIds?.ToHashSet();
        return invoicedLessons
            .Where(lesson => IsInDateRange(lesson.StartsAt, from, to) || includedIds?.Contains(lesson.Id) == true)
            .OrderBy(lesson => lesson.StartsAt)
            .Select(lesson =>
            {
                var invoice = invoiceLinesByLessonId[lesson.Id];
                return new InvoiceLessonStatus(
                    lesson.Student.ShortName,
                    lesson.StartsAt,
                    invoice.PaymentStatus,
                    new InvoiceNumber(invoice.InvoiceNumber),
                    lesson.Id);
            })
            .ToArray();
    }

    public async Task<IReadOnlyList<IssuedInvoiceSummary>> ListInvoicesForStudentAsync(
        int studentId,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        return await context.IssuedInvoices
            .AsNoTracking()
            .Where(invoice => invoice.StudentId == studentId)
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

    public async Task<IReadOnlyList<EconomyInvoiceRecord>> ListIssuedInvoiceOverviewRowsAsync(
        int periodYear,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var yearStart = new DateOnly(periodYear, 1, 1);
        var nextYearStart = yearStart.AddYears(1);
        return await context.IssuedInvoices
            .AsNoTracking()
            .Where(invoice => invoice.IssueStatus == InvoiceIssueStatus.Issued)
            .Where(invoice => invoice.PeriodFrom >= yearStart && invoice.PeriodFrom < nextYearStart)
            .OrderBy(invoice => invoice.InvoiceNumber)
            .Select(invoice => new EconomyInvoiceRecord(
                new InvoiceNumber(invoice.InvoiceNumber),
                invoice.InvoiceDate,
                new InvoicePeriod(invoice.PeriodFrom, invoice.PeriodTo),
                invoice.PdfFilePath,
                new InvoiceTotals(
                    new MoneyAmount(invoice.TotalWithoutVatAmount, invoice.Currency),
                    new MoneyAmount(invoice.VatAmount, invoice.Currency),
                    new MoneyAmount(invoice.TotalAmount, invoice.Currency))))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> HasInvoiceForStudentInMonthAsync(
        int studentId,
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
                    invoice.StudentId == studentId &&
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

    public async Task<OperationResult<IssuedInvoice>> ReserveInvoiceAsync(
        InvoiceDraft draft,
        InvoiceTotals totals,
        CancellationToken cancellationToken)
    {
        var requestedLessonIds = GetDistinctPositiveLessonIds(draft.Lines.Select(line => line.LessonId));
        await using var context = createContext();
        var existingReservation = await FindMatchingIssuingInvoiceAsync(context, requestedLessonIds, cancellationToken);
        if (existingReservation is not null)
        {
            if (draft.Student.StudentId is { } studentId && existingReservation.StudentId != studentId)
            {
                return StudentIdentityMismatch();
            }

            return OperationResult<IssuedInvoice>.Success(ToDomain(existingReservation));
        }

        var lessonStudents = await context.Lessons
            .Where(lesson => requestedLessonIds.Contains(lesson.Id))
            .Select(lesson => lesson.StudentId)
            .ToArrayAsync(cancellationToken);
        var ownerIds = lessonStudents.Distinct().ToArray();
        if (lessonStudents.Length != requestedLessonIds.Length || ownerIds.Length != 1 ||
            (draft.Student.StudentId is { } requestedOwner && requestedOwner != ownerIds[0]))
        {
            return StudentIdentityMismatch();
        }

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
            lastPrintError: null,
            snapshotRevision: CreateSnapshotRevision());

        var exists = await context.IssuedInvoices.AnyAsync(
            storedInvoice => storedInvoice.InvoiceNumber == invoiceNumber.Value,
            cancellationToken);

        if (exists)
        {
            return OperationResult<IssuedInvoice>.Failure(
                reservedInvoice,
                new ValidationError("Invoice.NumberAlreadyExists", "An issued invoice with this number already exists."));
        }

        var entity = ToEntity(reservedInvoice);
        entity.StudentId = ownerIds[0];
        context.IssuedInvoices.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return OperationResult<IssuedInvoice>.Success(ToDomain(entity));
    }

    public async Task<OperationResult<IssuedInvoice>> FinalizeIssuedInvoiceWithLessonLocksAsync(
        InvoiceNumber invoiceNumber,
        string pdfFilePath,
        IReadOnlyList<int> lessonIds,
        CancellationToken cancellationToken)
    {
        var distinctLessonIds = lessonIds
            .Where(lessonId => lessonId > 0)
            .Distinct()
            .Order()
            .ToArray();

        await using var context = createContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
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

        if (invoice.PlannedPdfPath is not null &&
            !string.Equals(invoice.PlannedPdfPath, pdfFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<IssuedInvoice>.Failure(new ValidationError(
                "Invoice.PublishedPathConflict", "The final PDF path differs from the saved publication checkpoint."));
        }

        var invoiceLineLessonIds = invoice.Lines
            .Select(line => line.LessonId)
            .Distinct()
            .Order()
            .ToArray();
        if (invoiceLineLessonIds.Length == 0 ||
            invoiceLineLessonIds.Any(lessonId => lessonId <= 0) ||
            !invoiceLineLessonIds.SequenceEqual(distinctLessonIds))
        {
            return OperationResult<IssuedInvoice>.Failure(
                new ValidationError(
                    "Invoice.LessonIdentityMismatch",
                    "The invoice lines do not match the lessons being locked."));
        }

        var lessons = await context.Lessons
            .Where(lesson => distinctLessonIds.Contains(lesson.Id))
            .ToArrayAsync(cancellationToken);
        if (lessons.Length != distinctLessonIds.Length)
        {
            return OperationResult<IssuedInvoice>.Failure(
                new ValidationError(
                    "Invoice.LessonNotFound",
                    "One or more invoice lessons could not be found."));
        }

        var alreadyLockedLesson = lessons.FirstOrDefault(lesson =>
            lesson.InvoiceNumber is not null &&
            lesson.InvoiceNumber != invoiceNumber.Value);
        if (alreadyLockedLesson is not null)
        {
            return OperationResult<IssuedInvoice>.Failure(
                new ValidationError(
                    "Invoice.LessonAlreadyInvoiced",
                    "One or more invoice lessons are already assigned to another invoice."));
        }

        invoice.PdfFilePath = pdfFilePath;
        invoice.IssueStatus = InvoiceIssueStatus.Issued;
        foreach (var lesson in lessons)
        {
            // Issued invoice lines are authoritative; this cache supports lock/display queries.
            lesson.InvoiceNumber = invoiceNumber.Value;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return OperationResult<IssuedInvoice>.Success(ToDomain(invoice));
    }

    public async Task<OperationResult<IssuedInvoice>> MarkInvoicePdfPublishedAsync(
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

        if ((invoice.PlannedPdfPath is not null &&
                !string.Equals(invoice.PlannedPdfPath, pdfFilePath, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(invoice.PdfFilePath) &&
                !string.Equals(invoice.PdfFilePath, pdfFilePath, StringComparison.OrdinalIgnoreCase)))
        {
            return OperationResult<IssuedInvoice>.Failure(
                new ValidationError(
                    "Invoice.PublishedPathConflict",
                    "The reserved invoice already points to a different published PDF."));
        }

        invoice.PdfFilePath = pdfFilePath;
        await context.SaveChangesAsync(cancellationToken);

        return OperationResult<IssuedInvoice>.Success(ToDomain(invoice));
    }

    public async Task<OperationResult<IssuedInvoice>> ReviseInvoiceReservationAsync(
        IssuedInvoice savedInvoice, InvoiceDraft draft, InvoiceTotals totals, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var invoice = await FindEditableReservationAsync(context, savedInvoice, cancellationToken);
        if (invoice is null || (draft.Student.StudentId is { } owner && owner != invoice.StudentId) ||
            !GetDistinctPositiveLessonIds(invoice.Lines.Select(line => line.LessonId))
                .SequenceEqual(GetDistinctPositiveLessonIds(draft.Lines.Select(line => line.LessonId))))
        {
            return ReservationChanged();
        }

        var replacement = ToEntity(draft.Issue(savedInvoice.InvoiceNumber, totals, pdfFilePath: null,
            InvoiceIssueStatus.Issuing, snapshotRevision: CreateSnapshotRevision()));
        replacement.Id = invoice.Id;
        replacement.StudentId = invoice.StudentId;
        context.Entry(invoice).CurrentValues.SetValues(replacement);
        var linesByLessonId = replacement.Lines.ToDictionary(line => line.LessonId);
        foreach (var line in invoice.Lines)
        {
            var replacementLine = linesByLessonId[line.LessonId];
            replacementLine.Id = line.Id;
            replacementLine.IssuedInvoiceEntityId = invoice.Id;
            context.Entry(line).CurrentValues.SetValues(replacementLine);
        }

        return await SaveReservationUpdateAsync(context, invoice, cancellationToken);
    }

    public async Task<OperationResult<IssuedInvoice>> PrepareInvoicePublicationAsync(
        IssuedInvoice savedInvoice, InvoicePdfPublication publication, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var invoice = await FindEditableReservationAsync(context, savedInvoice, cancellationToken);
        if (invoice is null)
        {
            return ReservationChanged();
        }

        invoice.StagedPdfPath = publication.StagingPath;
        invoice.PlannedPdfPath = publication.FinalPath;
        invoice.PdfSha256 = publication.Sha256;
        invoice.SnapshotRevision = CreateSnapshotRevision();
        return await SaveReservationUpdateAsync(context, invoice, cancellationToken);
    }

    public async Task ReleaseInvoiceReservationAsync(
        InvoiceNumber invoiceNumber, CancellationToken cancellationToken, string? expectedRevision = null)
    {
        await using var context = createContext();
        var reservation = context.IssuedInvoices.Where(invoice =>
            invoice.InvoiceNumber == invoiceNumber.Value &&
            invoice.IssueStatus == InvoiceIssueStatus.Issuing &&
            invoice.PdfFilePath == string.Empty &&
            invoice.PlannedPdfPath == null);

        if (expectedRevision is not null)
        {
            reservation = reservation.Where(invoice => invoice.SnapshotRevision == expectedRevision);
        }

        await reservation.ExecuteDeleteAsync(cancellationToken);
    }

    private static Task<IssuedInvoiceEntity?> FindEditableReservationAsync(
        LedgerDbContext context, IssuedInvoice savedInvoice, CancellationToken cancellationToken) =>
        context.IssuedInvoices
            .Include(invoice => invoice.Lines)
            .SingleOrDefaultAsync(invoice =>
                invoice.InvoiceNumber == savedInvoice.InvoiceNumber.Value &&
                invoice.SnapshotRevision == savedInvoice.SnapshotRevision &&
                invoice.IssueStatus == InvoiceIssueStatus.Issuing &&
                invoice.PdfFilePath == string.Empty &&
                invoice.PlannedPdfPath == null,
                cancellationToken);

    private static async Task<OperationResult<IssuedInvoice>> SaveReservationUpdateAsync(
        LedgerDbContext context, IssuedInvoiceEntity invoice, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return OperationResult<IssuedInvoice>.Success(ToDomain(invoice));
        }
        catch (DbUpdateConcurrencyException)
        {
            return ReservationChanged();
        }
    }

    private static OperationResult<IssuedInvoice> StudentIdentityMismatch() =>
        OperationResult<IssuedInvoice>.Failure(new ValidationError(
            "Invoice.StudentIdentityMismatch", "All invoice lessons must belong to the selected student."));

    private static OperationResult<IssuedInvoice> ReservationChanged() =>
        OperationResult<IssuedInvoice>.Failure(new ValidationError(
            "Invoice.ReservationChanged", "The pending invoice changed. Reopen the invoice preview and try again."));

    private static string CreateSnapshotRevision() => Guid.NewGuid().ToString("N");

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
            StudentId = invoice.Student.StudentId,
            StudentShortName = invoice.Student.ShortName,
            StudentFullName = invoice.Student.FullName,
            StudentAddress = invoice.Student.Address,
            PriceAdjustmentAmount = invoice.PriceAdjustment?.Amount.Amount,
            PriceAdjustmentReason = invoice.PriceAdjustment?.Reason,
            SnapshotRevision = invoice.SnapshotRevision,
            StagedPdfPath = invoice.Publication?.StagingPath,
            PlannedPdfPath = invoice.Publication?.FinalPath,
            PdfSha256 = invoice.Publication?.Sha256,
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
                HasKnownLessonTime = line.HasKnownLessonTime,
                PlanName = line.PlanName,
                PlanCatalogId = line.PlanCatalogId,
                LessonId = line.LessonId,
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
                line.PlanCatalogId,
                line.HasKnownLessonTime,
                line.LessonId))
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
            new StudentSnapshot(entity.StudentShortName, entity.StudentFullName, entity.StudentAddress, entity.StudentId),
            new ReadOnlyCollection<IssuedInvoiceLine>(lines),
            new InvoiceTotals(
                new MoneyAmount(entity.TotalWithoutVatAmount, currency),
                new MoneyAmount(entity.VatAmount, currency),
                new MoneyAmount(entity.TotalAmount, currency)),
            pdfFilePath,
            entity.IssueStatus,
            entity.PrintStatus,
            entity.PrintedAt,
            entity.LastPrintError,
            entity.PriceAdjustmentAmount is { } amount && entity.PriceAdjustmentReason is { } reason
                ? new PriceAdjustment(new MoneyAmount(amount, currency), reason)
                : null,
            entity.SnapshotRevision,
            entity.StagedPdfPath is { } staged && entity.PlannedPdfPath is { } planned && entity.PdfSha256 is { } checksum
                ? new InvoicePdfPublication(staged, planned, checksum)
                : null);
    }

    private static string FormatLessonDateTime(DateTimeOffset lessonDateTime) =>
        lessonDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseLessonDateTime(string lessonDateTime) =>
        DateTimeOffset.ParseExact(lessonDateTime, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static bool IsInDateRange(DateTimeOffset startsAt, DateOnly from, DateOnly to)
    {
        var date = DateOnly.FromDateTime(startsAt.DateTime);
        return date >= from && date <= to;
    }

    private static int[] GetDistinctPositiveLessonIds(IEnumerable<int> lessonIds) =>
        lessonIds
            .Where(lessonId => lessonId > 0)
            .Distinct()
            .Order()
            .ToArray();

    private static async Task<IssuedInvoiceEntity?> FindMatchingIssuingInvoiceAsync(
        LedgerDbContext context,
        IReadOnlyList<int> requestedLessonIds,
        CancellationToken cancellationToken)
    {
        if (requestedLessonIds.Count == 0)
        {
            return null;
        }

        var issuingInvoices = await context.IssuedInvoices
            .Include(invoice => invoice.Lines)
            .Where(invoice => invoice.IssueStatus == InvoiceIssueStatus.Issuing)
            .OrderBy(invoice => invoice.InvoiceNumber)
            .ToArrayAsync(cancellationToken);

        return issuingInvoices.FirstOrDefault(invoice =>
            invoice.Lines
                .Select(line => line.LessonId)
                .Where(lessonId => lessonId > 0)
                .Distinct()
                .Order()
                .SequenceEqual(requestedLessonIds));
    }
}
