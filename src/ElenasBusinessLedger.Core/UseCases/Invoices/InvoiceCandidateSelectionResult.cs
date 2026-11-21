namespace ElenasBusinessLedger.Core.UseCases.Invoices;

public sealed record InvoiceCandidateSelectionResult(
    IReadOnlyList<string> CandidateStudentShortNames,
    IReadOnlyList<string> AllStudentShortNames,
    string? PreselectedStudentShortName);
