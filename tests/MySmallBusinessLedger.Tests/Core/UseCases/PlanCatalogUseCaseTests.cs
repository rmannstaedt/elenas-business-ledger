using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.UseCases.Plans;

namespace MySmallBusinessLedger.Tests.Core.UseCases;

public sealed class PlanCatalogUseCaseTests
{
    [Fact]
    public async Task CreateDraftPlanCreatesEditableUnassignableDraft()
    {
        var repository = new FakePlanCatalogRepository();
        var useCase = new CreateDraftPlanUseCase(repository);

        var result = await useCase.ExecuteAsync(CreateInput(name: "Standard"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(PlanCatalogState.Draft, result.Value!.State);
        Assert.Equal("Standard", result.Value.Name);
        Assert.False(result.Value.CanBeAssigned);
        Assert.False(result.Value.IsImmutable);
        Assert.Single(repository.Plans);
    }

    [Fact]
    public async Task CreateDraftPlanRejectsInvalidFields()
    {
        var repository = new FakePlanCatalogRepository();
        var useCase = new CreateDraftPlanUseCase(repository);

        var result = await useCase.ExecuteAsync(CreateInput(name: "", priceWithoutVat: -1m), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Plan.NameRequired");
        Assert.Empty(repository.Plans);
    }

    [Fact]
    public async Task UpdateDraftPlanChangesDraftFields()
    {
        var repository = new FakePlanCatalogRepository();
        var draft = repository.Seed(CreatePlan("Standard", PlanCatalogState.Draft));
        var useCase = new UpdateDraftPlanUseCase(repository);

        var result = await useCase.ExecuteAsync(draft.Id, CreateInput(name: "Premium", priceWithoutVat: 500m), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Premium", result.Value!.Name);
        Assert.Equal(500m, result.Value.PriceWithoutVat);
    }

    [Fact]
    public async Task UpdateDraftPlanRejectsActiveAndArchivedPlans()
    {
        var repository = new FakePlanCatalogRepository();
        var active = repository.Seed(CreatePlan("Standard", PlanCatalogState.Active));
        var archived = repository.Seed(CreatePlan("Old Standard", PlanCatalogState.Archived));
        var useCase = new UpdateDraftPlanUseCase(repository);

        var activeResult = await useCase.ExecuteAsync(active.Id, CreateInput(name: "Changed"), CancellationToken.None);
        var archivedResult = await useCase.ExecuteAsync(archived.Id, CreateInput(name: "Changed Again"), CancellationToken.None);

        Assert.False(activeResult.Succeeded);
        Assert.Contains(activeResult.Errors, error => error.Code == "Plan.Immutable");
        Assert.False(archivedResult.Succeeded);
        Assert.Contains(archivedResult.Errors, error => error.Code == "Plan.Immutable");
        Assert.Equal("Standard", repository.Plans.Single(plan => plan.Id == active.Id).Name);
        Assert.Equal("Old Standard", repository.Plans.Single(plan => plan.Id == archived.Id).Name);
    }

    [Fact]
    public async Task DeleteDraftPlanDeletesOnlyDraftPlans()
    {
        var repository = new FakePlanCatalogRepository();
        var draft = repository.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        var active = repository.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var useCase = new DeleteDraftPlanUseCase(repository);

        var draftResult = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);
        var activeResult = await useCase.ExecuteAsync(active.Id, CancellationToken.None);

        Assert.True(draftResult.Succeeded);
        Assert.DoesNotContain(repository.Plans, plan => plan.Id == draft.Id);
        Assert.False(activeResult.Succeeded);
        Assert.Contains(activeResult.Errors, error => error.Code == "Plan.DeleteRequiresDraft");
        Assert.Contains(repository.Plans, plan => plan.Id == active.Id);
    }

    [Fact]
    public async Task PublishPlanMovesDraftOrArchivedPlanToActive()
    {
        var repository = new FakePlanCatalogRepository();
        var draft = repository.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        var archived = repository.Seed(CreatePlan("Archived", PlanCatalogState.Archived));
        var useCase = new PublishPlanUseCase(repository);

        var draftResult = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);
        var archivedResult = await useCase.ExecuteAsync(archived.Id, CancellationToken.None);

        Assert.True(draftResult.Succeeded);
        Assert.Equal(PlanCatalogState.Active, draftResult.Value!.State);
        Assert.True(draftResult.Value.IsImmutable);
        Assert.True(draftResult.Value.CanBeAssigned);
        Assert.True(archivedResult.Succeeded);
        Assert.Equal(PlanCatalogState.Active, archivedResult.Value!.State);
    }

    [Fact]
    public async Task PublishPlanRejectsAlreadyActivePlan()
    {
        var repository = new FakePlanCatalogRepository();
        var active = repository.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var useCase = new PublishPlanUseCase(repository);

        var result = await useCase.ExecuteAsync(active.Id, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Plan.PublishRequiresDraftOrArchived");
    }

    [Fact]
    public async Task ArchivePlanMovesOnlyActivePlanToArchived()
    {
        var repository = new FakePlanCatalogRepository();
        var active = repository.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var draft = repository.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        var useCase = new ArchivePlanUseCase(repository);

        var activeResult = await useCase.ExecuteAsync(active.Id, CancellationToken.None);
        var draftResult = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.True(activeResult.Succeeded);
        Assert.Equal(PlanCatalogState.Archived, activeResult.Value!.State);
        Assert.False(activeResult.Value.CanBeAssigned);
        Assert.True(activeResult.Value.IsImmutable);
        Assert.False(draftResult.Succeeded);
        Assert.Contains(draftResult.Errors, error => error.Code == "Plan.ArchiveRequiresActive");
    }

    [Fact]
    public async Task ListAssignablePlansReturnsOnlyActivePlans()
    {
        var repository = new FakePlanCatalogRepository();
        repository.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        repository.Seed(CreatePlan("Active", PlanCatalogState.Active));
        repository.Seed(CreatePlan("Archived", PlanCatalogState.Archived));
        var useCase = new ListAssignablePlansUseCase(repository);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        var plan = Assert.Single(result.Value!);
        Assert.Equal("Active", plan.Name);
        Assert.True(plan.CanBeAssigned);
    }

    private static PlanDraftInput CreateInput(
        string name = "Standard",
        decimal priceWithoutVat = 400m) =>
        new(
            name,
            priceWithoutVat,
            LessonsPerWeek.One,
            DurationMinutes: 60,
            PlanFlexibility.MoveOrCancelByAgreement,
            NumberOfStudents: 1,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private static PlanCatalogItem CreatePlan(string name, PlanCatalogState state) =>
        new(
            Id: 0,
            name,
            PriceWithoutVat: 400m,
            LessonsPerWeek.One,
            DurationMinutes: 60,
            PlanFlexibility.MoveOrCancelByAgreement,
            NumberOfStudents: 1,
            EffectiveFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            state);

    private sealed class FakePlanCatalogRepository : IPlanCatalogRepository
    {
        private readonly List<PlanCatalogItem> plans = [];
        private int nextId = 1;

        public IReadOnlyList<PlanCatalogItem> Plans => plans;

        public PlanCatalogItem Seed(PlanCatalogItem plan)
        {
            var saved = plan with { Id = nextId++ };
            plans.Add(saved);
            return saved;
        }

        public Task<IReadOnlyList<PlanCatalogItem>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlanCatalogItem>>(plans.OrderBy(plan => plan.Name, StringComparer.CurrentCulture).ToArray());

        public Task<IReadOnlyList<PlanCatalogItem>> ListAssignableAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlanCatalogItem>>(plans.Where(plan => plan.CanBeAssigned).ToArray());

        public Task<PlanCatalogItem?> GetAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(plans.SingleOrDefault(plan => plan.Id == id));

        public Task<OperationResult<PlanCatalogItem>> SaveAsync(PlanCatalogItem plan, CancellationToken cancellationToken)
        {
            if (plan.Id == 0)
            {
                plan = plan with { Id = nextId++ };
                plans.Add(plan);
                return Task.FromResult(OperationResult<PlanCatalogItem>.Success(plan));
            }

            var index = plans.FindIndex(storedPlan => storedPlan.Id == plan.Id);
            if (index < 0)
            {
                return Task.FromResult(OperationResult<PlanCatalogItem>.Failure(
                    new ValidationError("Plan.NotFound", "Plan was not found.")));
            }

            plans[index] = plan;
            return Task.FromResult(OperationResult<PlanCatalogItem>.Success(plan));
        }

        public Task<OperationResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var removed = plans.RemoveAll(plan => plan.Id == id) > 0;
            return Task.FromResult(removed
                ? OperationResult<bool>.Success(true)
                : OperationResult<bool>.Failure(new ValidationError("Plan.NotFound", "Plan was not found.")));
        }
    }
}
