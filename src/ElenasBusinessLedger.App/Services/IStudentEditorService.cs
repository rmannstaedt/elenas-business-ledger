using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Students;
using ElenasBusinessLedger.Core.UseCases.Students;

namespace ElenasBusinessLedger.App.Services;

// Holds existing student details, plan periods, and schedule in the shape needed by the editor fields.
public sealed record StudentEditorSnapshot(
    int StudentId,
    string ShortName,
    string FullName,
    string Address,
    string Phone,
    string Email,
    AgeBracket AgeBracket,
    DateOnly? Birthdate,
    bool IsActive,
    IReadOnlyList<StudentPlanPeriodInput> PlanPeriods,
    IReadOnlyList<StudentScheduleInput> Schedule);

// Loads a student for editing and saves the edited profile through a validation result.
public interface IStudentEditorService
{
    Task<StudentEditorSnapshot?> LoadAsync(string shortName, CancellationToken cancellationToken);

    Task<OperationResult<string>> SaveAsync(SaveStudentRequest request, CancellationToken cancellationToken);
}
