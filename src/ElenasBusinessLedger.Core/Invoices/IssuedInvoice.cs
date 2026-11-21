namespace ElenasBusinessLedger.Core.Invoices;

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
        string? lastPrintError = null)
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
}
