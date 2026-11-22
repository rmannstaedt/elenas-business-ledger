namespace ElenasBusinessLedger.Infrastructure.Invoices.Pdf;

// Provides the selected invoice directory so staging and publication use a known destination.
public sealed record InvoiceFileStoreOptions(string InvoiceFolderPath);
