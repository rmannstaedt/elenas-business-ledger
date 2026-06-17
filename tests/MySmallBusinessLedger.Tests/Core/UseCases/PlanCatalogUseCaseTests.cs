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
        Assert.Contains(result.Errors, error => error.Code == "Plan.PriceInvalid");
        Assert.Empty(repository.Plans);
    }

    [Fact]
    public async Task CreateDraftPlanAllowsIncompleteMandatoryPublishFields()
    {
        var repository = new FakePlanCatalogRepository();
        var useCase = new CreateDraftPlanUseCase(repository);

        var result = await useCase.ExecuteAsync(CreateInput(name: "", priceWithoutVat: null), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(PlanCatalogState.Draft, result.Value!.State);
        Assert.Equal(string.Empty, result.Value.Name);
        Assert.Null(result.Value.PriceWithoutVat);
    }

    [Fact]
    public async Task UpdateDraftPlanChangesDraftFields()
    {
        var repository = new FakePlanCatalogRepository();
        var draft = repository.Seed(CreatePlan("Standard", PlanCatalogState.Draft));
        var retireOn = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var useCase = new UpdateDraftPlanUseCase(repository);

        var result = await useCase.ExecuteAsync(
            draft.Id,
            CreateInput(name: "Premium", priceWithoutVat: 500m, retireOn: retireOn),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Premium", result.Value!.Name);
        Assert.Equal(500m, result.Value.PriceWithoutVat);
        Assert.Equal("Paid monthly in advance", result.Value.PaymentTerms);
        Assert.Equal(retireOn, result.Value.RetireOn);
    }

    [Fact]
    public async Task UpdateDraftPlanRejectsActiveAndRetiredPlans()
    {
        var repository = new FakePlanCatalogRepository();
        var active = repository.Seed(CreatePlan("Standard", PlanCatalogState.Active));
        var retired = repository.Seed(CreatePlan("Old Standard", PlanCatalogState.Retired));
        var useCase = new UpdateDraftPlanUseCase(repository);

        var activeResult = await useCase.ExecuteAsync(active.Id, CreateInput(name: "Changed"), CancellationToken.None);
        var retiredResult = await useCase.ExecuteAsync(retired.Id, CreateInput(name: "Changed Again"), CancellationToken.None);

        Assert.False(activeResult.Succeeded);
        Assert.Contains(activeResult.Errors, error => error.Code == "Plan.Immutable");
        Assert.False(retiredResult.Succeeded);
        Assert.Contains(retiredResult.Errors, error => error.Code == "Plan.Immutable");
        Assert.Equal("Standard", repository.Plans.Single(plan => plan.Id == active.Id).Name);
        Assert.Equal("Old Standard", repository.Plans.Single(plan => plan.Id == retired.Id).Name);
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
    public async Task PublishPlanMovesDraftPlanToActive()
    {
        var repository = new FakePlanCatalogRepository();
        var draft = repository.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        var useCase = new PublishPlanUseCase(repository);

        var draftResult = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.True(draftResult.Succeeded);
        Assert.Equal(PlanCatalogState.Active, draftResult.Value!.State);
        Assert.True(draftResult.Value.IsImmutable);
        Assert.True(draftResult.Value.CanBeAssigned);
    }

    [Fact]
    public async Task PublishPlanRejectsDraftMissingMandatoryFields()
    {
        var repository = new FakePlanCatalogRepository();
        var draft = repository.Seed(CreatePlan("", PlanCatalogState.Draft) with
        {
            PriceWithoutVat = null,
            PaymentTerms = ""
        });
        var useCase = new PublishPlanUseCase(repository);

        var result = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Plan.NameRequired");
        Assert.Contains(result.Errors, error => error.Code == "Plan.PriceRequired");
        Assert.Contains(result.Errors, error => error.Code == "Plan.PaymentTermsRequired");
        Assert.Equal(PlanCatalogState.Draft, repository.Plans.Single(plan => plan.Id == draft.Id).State);
    }

    [Fact]
    public async Task PublishPlanRejectsAlreadyActiveAndRetiredPlans()
    {
        var repository = new FakePlanCatalogRepository();
        var active = repository.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var retired = repository.Seed(CreatePlan("Retired", PlanCatalogState.Retired));
        var useCase = new PublishPlanUseCase(repository);

        var activeResult = await useCase.ExecuteAsync(active.Id, CancellationToken.None);
        var retiredResult = await useCase.ExecuteAsync(retired.Id, CancellationToken.None);

        Assert.False(activeResult.Succeeded);
        Assert.Contains(activeResult.Errors, error => error.Code == "Plan.PublishRequiresDraft");
        Assert.False(retiredResult.Succeeded);
        Assert.Contains(retiredResult.Errors, error => error.Code == "Plan.PublishRequiresDraft");
    }

    [Fact]
    public async Task RetirePlanMovesOnlyActivePlanToRetired()
    {
        var repository = new FakePlanCatalogRepository();
        var active = repository.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var draft = repository.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        var useCase = new RetirePlanUseCase(repository);

        var activeResult = await useCase.ExecuteAsync(active.Id, CancellationToken.None);
        var draftResult = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.True(activeResult.Succeeded);
        Assert.Equal(PlanCatalogState.Retired, activeResult.Value!.State);
        Assert.False(activeResult.Value.CanBeAssigned);
        Assert.True(activeResult.Value.IsImmutable);
        Assert.False(draftResult.Succeeded);
        Assert.Contains(draftResult.Errors, error => error.Code == "Plan.RetireRequiresActive");
    }

    [Fact]
    public async Task ReactivatePlanMovesRetiredPlanToActiveAndClearsRetireOn()
    {
        var repository = new FakePlanCatalogRepository();
        var retired = repository.Seed(CreatePlan("Retired", PlanCatalogState.Retired) with
        {
            RetireOn = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
        var useCase = new ReactivatePlanUseCase(repository);

        var result = await useCase.ExecuteAsync(retired.Id, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(PlanCatalogState.Active, result.Value!.State);
        Assert.Null(result.Value.RetireOn);
    }

    [Fact]
    public async Task UpdatePlanRetireOnAllowsDraftAndActiveButRejectsRetired()
    {
        var repository = new FakePlanCatalogRepository();
        var draft = repository.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        var active = repository.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var retired = repository.Seed(CreatePlan("Retired", PlanCatalogState.Retired));
        var retireOn = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var useCase = new UpdatePlanRetireOnUseCase(repository);

        var draftResult = await useCase.ExecuteAsync(draft.Id, retireOn, CancellationToken.None);
        var activeResult = await useCase.ExecuteAsync(active.Id, retireOn, CancellationToken.None);
        var retiredResult = await useCase.ExecuteAsync(retired.Id, retireOn, CancellationToken.None);

        Assert.True(draftResult.Succeeded);
        Assert.Equal(retireOn, draftResult.Value!.RetireOn);
        Assert.True(activeResult.Succeeded);
        Assert.Equal(retireOn, activeResult.Value!.RetireOn);
        Assert.False(retiredResult.Succeeded);
        Assert.Contains(retiredResult.Errors, error => error.Code == "Plan.RetireOnImmutable");
    }

    [Fact]
    public async Task RetireDuePlansRetiresOnlyActivePlansWhoseRetireOnHasArrived()
    {
        var repository = new FakePlanCatalogRepository();
        var dueActive = repository.Seed(CreatePlan("Due", PlanCatalogState.Active) with
        {
            RetireOn = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
        });
        var futureActive = repository.Seed(CreatePlan("Future", PlanCatalogState.Active) with
        {
            RetireOn = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)
        });
        var dueDraft = repository.Seed(CreatePlan("Due draft", PlanCatalogState.Draft) with
        {
            RetireOn = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
        });
        var useCase = new RetireDuePlansUseCase(repository);

        var result = await useCase.ExecuteAsync(new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value);
        Assert.Equal(PlanCatalogState.Retired, repository.Plans.Single(plan => plan.Id == dueActive.Id).State);
        Assert.Equal(PlanCatalogState.Active, repository.Plans.Single(plan => plan.Id == futureActive.Id).State);
        Assert.Equal(PlanCatalogState.Draft, repository.Plans.Single(plan => plan.Id == dueDraft.Id).State);
    }

    [Fact]
    public async Task ListAssignablePlansReturnsOnlyActivePlans()
    {
        var repository = new FakePlanCatalogRepository();
        repository.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        repository.Seed(CreatePlan("Active", PlanCatalogState.Active));
        repository.Seed(CreatePlan("Retired", PlanCatalogState.Retired));
        var useCase = new ListAssignablePlansUseCase(repository);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        var plan = Assert.Single(result.Value!);
        Assert.Equal("Active", plan.Name);
        Assert.True(plan.CanBeAssigned);
    }

    private static PlanDraftInput CreateInput(
        string name = "Standard",
        decimal? priceWithoutVat = 400m,
        DateTimeOffset? retireOn = null) =>
        new(
            name,
            priceWithoutVat,
            LessonsPerWeek.One,
            DurationMinutes: 60,
            PlanFlexibility.MoveOrCancelByAgreement,
            NumberOfStudents: 1,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            "Paid monthly in advance",
            retireOn);

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
            PaymentTerms: "Paid monthly in advance",
            RetireOn: null,
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
