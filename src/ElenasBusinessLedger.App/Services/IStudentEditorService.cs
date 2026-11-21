using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Students;
using ElenasBusinessLedger.Core.UseCases.Students;

namespace ElenasBusinessLedger.App.Services;

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

public interface IStudentEditorService
{
    Task<StudentEditorSnapshot?> LoadAsync(string shortName, CancellationToken cancellationToken);

    Task<OperationResult<string>> SaveAsync(SaveStudentRequest request, CancellationToken cancellationToken);
}
