namespace ElenasBusinessLedger.Core.Invoices;

// Carries the stored billing snapshot in both pending and issued states. Recovery, printing, and reporting use these saved values.
public sealed class IssuedInvoice
{
    public IssuedInvoice(
        InvoiceNumber invoiceNumber,
        DateOnly invoiceDate,
        InvoicePeriod period,
        CompanySnapshot company,
        StudentSnapshot student,
        IReadOnlyList<IssuedInvoiceLine> lines,
        InvoiceTotals totals,
        string? pdfFilePath,
        InvoiceIssueStatus issueStatus = InvoiceIssueStatus.Issued,
        InvoicePrintStatus printStatus = InvoicePrintStatus.NotPrinted,
        DateTimeOffset? printedAt = null,
        string? lastPrintError = null,
        PriceAdjustment? priceAdjustment = null,
        string? snapshotRevision = null,
        InvoicePdfPublication? publication = null)
    {
        InvoiceNumber = invoiceNumber;
        InvoiceDate = invoiceDate;
        Period = period;
        Company = company;
        Student = student;
        Lines = lines;
        Totals = totals;
        PdfFilePath = pdfFilePath;
        IssueStatus = issueStatus;
        PrintStatus = printStatus;
        PrintedAt = printedAt;
        LastPrintError = lastPrintError;
        PriceAdjustment = priceAdjustment;
        SnapshotRevision = snapshotRevision;
        Publication = publication;
    }

    public InvoiceNumber InvoiceNumber { get; }

    public DateOnly InvoiceDate { get; }

    public InvoicePeriod Period { get; }

    public CompanySnapshot Company { get; }

    public StudentSnapshot Student { get; }

    public IReadOnlyList<IssuedInvoiceLine> Lines { get; }

    public InvoiceTotals Totals { get; }

    public string? PdfFilePath { get; }

    public InvoiceIssueStatus IssueStatus { get; }

    public InvoicePrintStatus PrintStatus { get; }

    public DateTimeOffset? PrintedAt { get; }

    public string? LastPrintError { get; }

    public PriceAdjustment? PriceAdjustment { get; }

    public string? SnapshotRevision { get; }

    public InvoicePdfPublication? Publication { get; }

    /// <summary>Recreates the PDF input from the stored details without reloading student or plan data.</summary>
    public InvoiceDraft CreateSavedDraft() => InvoiceDraft.Create(
        InvoiceDate,
        Company,
        Student,
        Lines.Select(line => new InvoiceLineDraft(
            line.LessonDateTime,
            line.PlanName,
            line.PriceWithoutVat,
            line.PlanCatalogId,
            line.LessonId,
            line.HasKnownLessonTime)).ToArray(),
        PriceAdjustment,
        Period);
}
