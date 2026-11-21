using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

// Supplies candidate choices and a suggested student so the invoice dialog can start with a selection.
public sealed record InvoiceCandidateSelectionResult(
    IReadOnlyList<string> CandidateStudentShortNames,
    IReadOnlyList<string> AllStudentShortNames,
    string? PreselectedStudentShortName,
    IReadOnlyList<StudentChoice>? StudentChoices = null,
    IReadOnlyList<StudentChoice>? CandidateChoices = null,
    int? PreselectedStudentId = null);
