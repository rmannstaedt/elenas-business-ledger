using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Students;
using ElenaSmallBusinessLedger.Core.UseCases.Students;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed record StudentEditorSnapshot(
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
