using System.Collections.ObjectModel;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Company;
using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;

namespace ElenasBusinessLedger.Core.Invoices;

// Holds proposed lines and billing details so the preview can calculate amounts before reserving a number.
public sealed class InvoiceDraft
{
    private InvoiceDraft(
        DateOnly invoiceDate,
        InvoicePeriod period,
        CompanySnapshot companySnapshot,
        StudentSnapshot studentSnapshot,
        IReadOnlyList<InvoiceLineDraft> lines,
        PriceAdjustment? priceAdjustment)
    {
        InvoiceDate = invoiceDate;
        Period = period;
        Company = companySnapshot;
        Student = studentSnapshot;
        Lines = lines;
        PriceAdjustment = priceAdjustment;
    }

    public DateOnly InvoiceDate { get; }

    public InvoicePeriod Period { get; }

    public CompanySnapshot Company { get; }

    public StudentSnapshot Student { get; }

    public IReadOnlyList<InvoiceLineDraft> Lines { get; }

    public PriceAdjustment? PriceAdjustment { get; }

    // Builds a draft for callers that treat invalid or incomplete billing data as an error.
    public static InvoiceDraft Create(
        DateOnly invoiceDate,
        CompanySnapshot companySnapshot,
        StudentSnapshot studentSnapshot,
        IReadOnlyList<InvoiceLineDraft> lines,
        PriceAdjustment? priceAdjustment,
        InvoicePeriod? period = null)
    {
        var result = TryCreate(invoiceDate, companySnapshot, studentSnapshot, lines, priceAdjustment, period);
        return result.Value ?? throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Message)));
    }

    // Validates draft lines, period, and adjustments so the editor can surface errors before issuance.
    public static ValidationResult<InvoiceDraft> TryCreate(
        DateOnly invoiceDate,
        CompanySnapshot companySnapshot,
        StudentSnapshot studentSnapshot,
        IReadOnlyList<InvoiceLineDraft> lines,
        PriceAdjustment? priceAdjustment,
        InvoicePeriod? period = null)
    {
        if (lines.Count == 0)
        {
            return ValidationResult<InvoiceDraft>.Failure(new ValidationError(
                "Invoice.NoLines",
                "An invoice requires at least one lesson."));
        }

        if (priceAdjustment is not null && string.IsNullOrWhiteSpace(priceAdjustment.Reason))
        {
            return ValidationResult<InvoiceDraft>.Failure(new ValidationError(
                "Invoice.PriceAdjustmentReasonRequired",
                "A price adjustment requires a reason."));
        }

        var invoicePeriod = period ?? InvoicePeriod.FromLines(lines);
        if (invoicePeriod.From > invoicePeriod.To)
        {
            return ValidationResult<InvoiceDraft>.Failure(new ValidationError(
                "Invoice.InvalidPeriod",
                "The invoice period start date must be on or before the end date."));
        }

        return ValidationResult<InvoiceDraft>.Success(new InvoiceDraft(
            invoiceDate,
            invoicePeriod,
            companySnapshot,
            studentSnapshot,
            lines.ToArray(),
            priceAdjustment));
    }

    // Applies the adjustment and determines VAT from the start of the invoice period.
    public InvoiceTotals CalculateTotals(CompanyProfile company)
    {
        var subtotal = Lines.Aggregate(MoneyAmount.Dkk(0m), (total, line) => total + line.PriceWithoutVat);
        var totalWithoutVat = PriceAdjustment is null ? subtotal : subtotal + PriceAdjustment.Amount;
        var vat = company.ChargesVatOn(Period.From) ? totalWithoutVat * 0.25m : MoneyAmount.Dkk(0m);

        return new InvoiceTotals(totalWithoutVat, vat, totalWithoutVat + vat);
    }

    // Copies the billing details and lesson IDs into a snapshot under the supplied invoice number.
    public IssuedInvoice Issue(
        InvoiceNumber invoiceNumber,
        InvoiceTotals totals,
        string? pdfFilePath,
        InvoiceIssueStatus issueStatus = InvoiceIssueStatus.Issued,
        InvoicePrintStatus printStatus = InvoicePrintStatus.NotPrinted,
        DateTimeOffset? printedAt = null,
        string? lastPrintError = null,
        string? snapshotRevision = null,
        InvoicePdfPublication? publication = null)
    {
        var issuedLines = Lines
            .Select(line => new IssuedInvoiceLine(
                line.LessonDateTime,
                line.PlanName,
                line.PriceWithoutVat,
                line.PlanCatalogId,
                line.HasKnownLessonTime,
                line.LessonId))
            .ToArray();

        return new IssuedInvoice(
            invoiceNumber,
            InvoiceDate,
            Period,
            Company,
            Student,
            new ReadOnlyCollection<IssuedInvoiceLine>(issuedLines),
            totals,
            pdfFilePath,
            issueStatus,
            printStatus,
            printedAt,
            lastPrintError,
            PriceAdjustment,
            snapshotRevision,
            publication);
    }
}
