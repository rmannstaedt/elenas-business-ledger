using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.UseCases.Students;

namespace ElenasBusinessLedger.App.Services;

// Maps saved students into editor fields and delegates validation and persistence to Core.
public sealed class CoreStudentEditorService : IStudentEditorService
{
    private readonly IStudentRepository studentRepository;
    private readonly SaveStudentUseCase saveStudentUseCase;

    public CoreStudentEditorService(IStudentRepository studentRepository)
    {
        this.studentRepository = studentRepository;
        saveStudentUseCase = new SaveStudentUseCase(studentRepository);
    }

    // Copies the stored student's fields, plan periods, and schedule into the shape used by the details form.
    public async Task<StudentEditorSnapshot?> LoadAsync(string shortName, CancellationToken cancellationToken)
    {
        var student = await studentRepository.GetByShortNameAsync(shortName, cancellationToken);
        return student is null
            ? null
            : new StudentEditorSnapshot(
                student.Id,
                student.Student.ShortName,
                student.Student.FullName,
                student.Student.Address,
                student.Student.Phone,
                student.Student.Email,
                student.Student.AgeBracket,
                student.Student.Birthdate,
                student.Student.IsActive,
                student.PlanPeriods.Select(plan => new StudentPlanPeriodInput(
                    plan.Plan.Name,
                    plan.Plan.PriceWithoutVat,
                    plan.Plan.LessonsPerWeek,
                    plan.Plan.DurationMinutes,
                    plan.Plan.Flexibility,
                    plan.Plan.NumberOfStudents,
                    plan.Plan.EffectiveFrom,
                    plan.StartsOn,
                    plan.EndsOn,
                    plan.Plan.PaymentTerms,
                    plan.Plan.PlanCatalogId)).ToArray(),
                student.Schedule.Select(schedule => new StudentScheduleInput(schedule.Weekday, schedule.StartsAt)).ToArray());
    }

    // Validates and saves the edited student, returning the saved label for the selection refresh.
    public async Task<OperationResult<string>> SaveAsync(SaveStudentRequest request, CancellationToken cancellationToken)
    {
        var result = await saveStudentUseCase.ExecuteAsync(request, cancellationToken);
        return result.Succeeded
            ? OperationResult<string>.Success(result.Value!.Student.ShortName)
            : OperationResult<string>.Failure(result.Errors.ToArray());
    }
}
