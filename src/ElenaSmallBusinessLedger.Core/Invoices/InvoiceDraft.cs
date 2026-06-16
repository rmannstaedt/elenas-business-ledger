using System.Collections.ObjectModel;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Company;
using MoneyAmount = ElenaSmallBusinessLedger.Core.Money.Money;

namespace ElenaSmallBusinessLedger.Core.Invoices;

public sealed class InvoiceDraft
{
    private InvoiceDraft(
        DateOnly invoiceDate,
        CompanySnapshot companySnapshot,
        StudentSnapshot studentSnapshot,
        IReadOnlyList<InvoiceLineDraft> lines,
        PriceAdjustment? priceAdjustment)
    {
        InvoiceDate = invoiceDate;
        Company = companySnapshot;
        Student = studentSnapshot;
        Lines = lines;
        PriceAdjustment = priceAdjustment;
    }

    public DateOnly InvoiceDate { get; }

    public CompanySnapshot Company { get; }

    public StudentSnapshot Student { get; }

    public IReadOnlyList<InvoiceLineDraft> Lines { get; }

    public PriceAdjustment? PriceAdjustment { get; }

    public static InvoiceDraft Create(
        DateOnly invoiceDate,
        CompanySnapshot companySnapshot,
        StudentSnapshot studentSnapshot,
        IReadOnlyList<InvoiceLineDraft> lines,
        PriceAdjustment? priceAdjustment)
    {
        var result = TryCreate(invoiceDate, companySnapshot, studentSnapshot, lines, priceAdjustment);
        return result.Value ?? throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Message)));
    }

    public static ValidationResult<InvoiceDraft> TryCreate(
        DateOnly invoiceDate,
        CompanySnapshot companySnapshot,
        StudentSnapshot studentSnapshot,
        IReadOnlyList<InvoiceLineDraft> lines,
        PriceAdjustment? priceAdjustment)
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

        return ValidationResult<InvoiceDraft>.Success(new InvoiceDraft(
            invoiceDate,
            companySnapshot,
            studentSnapshot,
            lines.ToArray(),
            priceAdjustment));
    }

    public InvoiceTotals CalculateTotals(CompanyProfile company)
    {
        var subtotal = Lines.Aggregate(MoneyAmount.Dkk(0m), (total, line) => total + line.PriceWithoutVat);
        var totalWithoutVat = PriceAdjustment is null ? subtotal : subtotal + PriceAdjustment.Amount;
        var vat = company.ChargesVatOn(InvoiceDate) ? totalWithoutVat * 0.25m : MoneyAmount.Dkk(0m);

        return new InvoiceTotals(totalWithoutVat, vat, totalWithoutVat + vat);
    }

    public IssuedInvoice Issue(InvoiceNumber invoiceNumber, InvoiceTotals totals, string pdfFilePath)
    {
        var issuedLines = Lines
            .Select(line => new IssuedInvoiceLine(line.LessonDateTime, line.PlanName, line.PriceWithoutVat))
            .ToArray();

        return new IssuedInvoice(
            invoiceNumber,
            InvoiceDate,
            Company,
            Student,
            new ReadOnlyCollection<IssuedInvoiceLine>(issuedLines),
            totals,
            pdfFilePath);
    }
}
