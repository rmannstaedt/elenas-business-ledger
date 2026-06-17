namespace MySmallBusinessLedger.Core.Invoices;

public sealed class IssuedInvoice
{
    public IssuedInvoice(
        InvoiceNumber invoiceNumber,
        DateOnly invoiceDate,
        CompanySnapshot company,
        StudentSnapshot student,
        IReadOnlyList<IssuedInvoiceLine> lines,
        InvoiceTotals totals,
        string pdfFilePath)
    {
        InvoiceNumber = invoiceNumber;
        InvoiceDate = invoiceDate;
        Company = company;
        Student = student;
        Lines = lines;
        Totals = totals;
        PdfFilePath = pdfFilePath;
    }

    public InvoiceNumber InvoiceNumber { get; }

    public DateOnly InvoiceDate { get; }

    public CompanySnapshot Company { get; }

    public StudentSnapshot Student { get; }

    public IReadOnlyList<IssuedInvoiceLine> Lines { get; }

    public InvoiceTotals Totals { get; }

    public string PdfFilePath { get; }
}
