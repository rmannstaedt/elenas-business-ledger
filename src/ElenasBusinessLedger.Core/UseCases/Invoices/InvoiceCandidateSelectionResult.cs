using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

public sealed record InvoiceCandidateSelectionResult(
    IReadOnlyList<string> CandidateStudentShortNames,
    IReadOnlyList<string> AllStudentShortNames,
    string? PreselectedStudentShortName,
    IReadOnlyList<StudentChoice>? StudentChoices = null,
    IReadOnlyList<StudentChoice>? CandidateChoices = null,
    int? PreselectedStudentId = null);
