using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.Students;
using MySmallBusinessLedger.Core.UseCases.Students;

namespace MySmallBusinessLedger.Tests.Core.UseCases;

public sealed class StudentUseCaseTests
{
    [Fact]
    public async Task SaveStudentPersistsStudentPlanAndSchedule()
    {
        var repository = new FakeStudentRepository();
        var useCase = new SaveStudentUseCase(repository);

        var result = await useCase.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Anna", repository.SavedStudent!.Student.ShortName);
        Assert.Single(repository.SavedStudent.PlanPeriods);
        Assert.Single(repository.SavedStudent.Schedule);
        Assert.Equal(DayOfWeek.Tuesday, repository.SavedStudent.Schedule[0].Weekday);
        Assert.Equal(new TimeOnly(15, 0), repository.SavedStudent.Schedule[0].StartsAt);
    }

    [Fact]
    public async Task SaveStudentRejectsOverlappingPlanPeriods()
    {
        var repository = new FakeStudentRepository();
        var useCase = new SaveStudentUseCase(repository);
        var request = CreateRequest(planPeriods:
        [
            CreatePlanPeriod("Standard", startsOn: new DateOnly(2026, 1, 1), endsOn: new DateOnly(2026, 6, 30), planCatalogId: 1),
            CreatePlanPeriod("Extended", startsOn: new DateOnly(2026, 6, 1), endsOn: null, planCatalogId: 2)
        ]);

        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "StudentPlan.OverlappingPeriods");
        Assert.Null(repository.SavedStudent);
    }

    private static SaveStudentRequest CreateRequest(
        IReadOnlyList<StudentPlanPeriodInput>? planPeriods = null) =>
        new(
            StudentId: 0,
            ShortName: "Anna",
            FullName: "Anna Andersen",
            Address: "Testvej 1",
            Phone: "12345678",
            Email: "anna@example.test",
            AgeBracket.Standard,
            Birthdate: null,
            IsActive: true,
            PlanPeriods: planPeriods ?? [CreatePlanPeriod("Standard", new DateOnly(2026, 1, 1), null, planCatalogId: 1)],
            Schedule:
            [
                new StudentScheduleInput(DayOfWeek.Tuesday, new TimeOnly(15, 0))
            ]);

    private static StudentPlanPeriodInput CreatePlanPeriod(
        string name,
        DateOnly startsOn,
        DateOnly? endsOn,
        int planCatalogId) =>
        new(
            PlanName: name,
            PriceWithoutVat: 400m,
            LessonsPerWeek.One,
            DurationMinutes: 60,
            PlanFlexibility.MoveOrCancelByAgreement,
            NumberOfStudents: 1,
            PlanEffectiveFrom: new DateTimeOffset(startsOn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            StartsOn: startsOn,
            EndsOn: endsOn,
            PlanCatalogId: planCatalogId);

    private sealed class FakeStudentRepository : IStudentRepository
    {
        public StudentRecord? SavedStudent { get; private set; }

        public Task<IReadOnlyList<StudentRecord>> ListStudentsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StudentRecord>>([]);

        public Task<IReadOnlyList<StudentRecord>> ListActiveStudentsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StudentRecord>>([]);

        public Task<StudentRecord?> GetByShortNameAsync(string shortName, CancellationToken cancellationToken) =>
            Task.FromResult<StudentRecord?>(null);

        public Task<OperationResult<StudentRecord>> SaveAsync(StudentRecord student, CancellationToken cancellationToken)
        {
            SavedStudent = student;
            return Task.FromResult(OperationResult<StudentRecord>.Success(student));
        }
    }
}
