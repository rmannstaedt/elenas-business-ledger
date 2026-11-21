namespace ElenasBusinessLedger.Core.Invoices;

public sealed record InvoicePdfPublication(string StagingPath, string FinalPath, string Sha256);
