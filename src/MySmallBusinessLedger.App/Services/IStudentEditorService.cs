using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Students;
using MySmallBusinessLedger.Core.UseCases.Students;

namespace MySmallBusinessLedger.App.Services;

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
