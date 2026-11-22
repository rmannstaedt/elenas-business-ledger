namespace ElenasBusinessLedger.Core.Invoices;

// Saves the staging path, destination, and checksum so a retry can recognize the same PDF after a move.
public sealed record InvoicePdfPublication(string StagingPath, string FinalPath, string Sha256);
