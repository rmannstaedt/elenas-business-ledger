using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Students;

// Validates and normalizes student edits before saving the profile, plan periods, and recurring schedule together.
public sealed class SaveStudentUseCase
{
    private readonly IStudentRepository studentRepository;

    public SaveStudentUseCase(IStudentRepository studentRepository)
    {
        this.studentRepository = studentRepository;
    }

    // Normalizes the student, schedule, and plan periods before saving them as one student record.
    public Task<OperationResult<StudentRecord>> ExecuteAsync(
        SaveStudentRequest request,
        CancellationToken cancellationToken)
    {
        var studentResult = Student.Create(
            request.ShortName.Trim(),
            request.FullName.Trim(),
            request.Address.Trim(),
            request.Phone.Trim(),
            request.Email.Trim(),
            request.AgeBracket,
            request.Birthdate,
            request.IsActive);
        if (!studentResult.Succeeded)
        {
            return Task.FromResult(OperationResult<StudentRecord>.Failure(studentResult.Errors.ToArray()));
        }

        var planPeriods = request.PlanPeriods
            .Select(planPeriod => new StudentPlanPeriod(
                new Plan(
                    planPeriod.PlanName.Trim(),
                    planPeriod.PriceWithoutVat,
                    planPeriod.LessonsPerWeek,
                    planPeriod.DurationMinutes,
                    planPeriod.Flexibility,
                    planPeriod.NumberOfStudents,
                    planPeriod.PlanEffectiveFrom,
                    planPeriod.PaymentTerms.Trim(),
                    planPeriod.PlanCatalogId),
                planPeriod.StartsOn,
                planPeriod.EndsOn))
            .OrderBy(planPeriod => planPeriod.StartsOn)
            .ToArray();

        var validationError = ValidatePlanPeriods(planPeriods);
        if (validationError is not null)
        {
            return Task.FromResult(OperationResult<StudentRecord>.Failure(validationError));
        }

        var schedule = request.Schedule
            .Distinct()
            .OrderBy(entry => entry.Weekday)
            .ThenBy(entry => entry.StartsAt)
            .Select(entry => new StudentScheduleEntry(entry.Weekday, entry.StartsAt))
            .ToArray();

        var record = new StudentRecord(request.StudentId, studentResult.Value!, planPeriods, schedule);
        return studentRepository.SaveAsync(record, cancellationToken);
    }

    private static ValidationError? ValidatePlanPeriods(IReadOnlyList<StudentPlanPeriod> planPeriods)
    {
        if (planPeriods.Count == 0)
        {
            return new ValidationError("StudentPlan.Required", "At least one student plan is required.");
        }

        if (planPeriods.Any(planPeriod => planPeriod.Plan.PlanCatalogId <= 0))
        {
            return new ValidationError("StudentPlan.CatalogPlanRequired", "A catalog plan is required.");
        }

        for (var index = 0; index < planPeriods.Count; index++)
        {
            for (var otherIndex = index + 1; otherIndex < planPeriods.Count; otherIndex++)
            {
                if (Overlaps(planPeriods[index], planPeriods[otherIndex]))
                {
                    return new ValidationError(
                        "StudentPlan.OverlappingPeriods",
                        "Only one student plan can be active on any date.");
                }
            }
        }

        return null;
    }

    private static bool Overlaps(StudentPlanPeriod left, StudentPlanPeriod right)
    {
        var leftEnds = left.EndsOn ?? DateOnly.MaxValue;
        var rightEnds = right.EndsOn ?? DateOnly.MaxValue;
        return left.StartsOn <= rightEnds && right.StartsOn <= leftEnds;
    }
}
