namespace ElenasBusinessLedger.Core.Invoices;

// Copies recipient details into the invoice while carrying the stable student ID for history and ownership.
public sealed record StudentSnapshot(string ShortName, string FullName, string Address, int? StudentId = null);
