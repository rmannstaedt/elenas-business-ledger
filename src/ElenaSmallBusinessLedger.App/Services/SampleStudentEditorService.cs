using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Plans;
using ElenaSmallBusinessLedger.Core.Students;
using ElenaSmallBusinessLedger.Core.UseCases.Students;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class SampleStudentEditorService : IStudentEditorService
{
    private readonly Dictionary<string, StudentEditorSnapshot> students = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Anna"] = new StudentEditorSnapshot(
            "Anna",
            "Anna Andersen",
            "Testvej 1",
            "12345678",
            "anna@example.test",
            AgeBracket.Standard,
            Birthdate: null,
            IsActive: true,
            PlanPeriods:
            [
                new StudentPlanPeriodInput(
                    "Standard",
                    400m,
                    LessonsPerWeek.One,
                    60,
                    PlanFlexibility.MoveOrCancelByAgreement,
                    1,
                    new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    new DateOnly(2026, 1, 1),
                    EndsOn: null)
            ],
            Schedule:
            [
                new StudentScheduleInput(DayOfWeek.Tuesday, new TimeOnly(15, 0))
            ])
    };

    public Task<StudentEditorSnapshot?> LoadAsync(string shortName, CancellationToken cancellationToken)
    {
        students.TryGetValue(shortName, out var student);
        return Task.FromResult(student);
    }

    public Task<OperationResult<string>> SaveAsync(SaveStudentRequest request, CancellationToken cancellationToken)
    {
        students[request.ShortName] = new StudentEditorSnapshot(
            request.ShortName,
            request.FullName,
            request.Address,
            request.Phone,
            request.Email,
            request.AgeBracket,
            request.Birthdate,
            request.IsActive,
            request.PlanPeriods,
            request.Schedule);

        return Task.FromResult(OperationResult<string>.Success(request.ShortName));
    }
}
