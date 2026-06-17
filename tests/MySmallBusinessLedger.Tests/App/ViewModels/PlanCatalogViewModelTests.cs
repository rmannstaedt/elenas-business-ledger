using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.UseCases.Plans;
using System.Windows;

namespace MySmallBusinessLedger.Tests.App.ViewModels;

public sealed class PlanCatalogViewModelTests
{
    [Fact]
    public async Task LoadListsAllPlansByState()
    {
        var service = new FakePlanCatalogService();
        service.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        service.Seed(CreatePlan("Active", PlanCatalogState.Active));
        service.Seed(CreatePlan("Archived", PlanCatalogState.Archived));
        var viewModel = new PlanCatalogViewModel(service);

        await viewModel.LoadAsync();

        Assert.Equal(3, viewModel.Plans.Count);
        Assert.Equal(["Draft", "Active", "Archived"], viewModel.Plans.Select(plan => plan.StateText).Distinct().ToArray());
        Assert.Equal("Draft", viewModel.SelectedPlan?.Name);
    }

    [Theory]
    [InlineData(PlanCatalogState.Draft, true, true, true, false)]
    [InlineData(PlanCatalogState.Active, false, false, false, true)]
    [InlineData(PlanCatalogState.Archived, false, false, true, false)]
    public async Task SelectedPlanExposesOnlyStateRelevantEditingAndActions(
        PlanCatalogState state,
        bool canEditFields,
        bool canDelete,
        bool canPublish,
        bool canArchive)
    {
        var service = new FakePlanCatalogService();
        service.Seed(CreatePlan("Selected", state));
        var viewModel = new PlanCatalogViewModel(service);

        await viewModel.LoadAsync();

        Assert.Equal(canEditFields, viewModel.CanEditSelectedPlanFields);
        Assert.Equal(canDelete, viewModel.CanDeleteSelectedPlan);
        Assert.Equal(canPublish, viewModel.CanPublishSelectedPlan);
        Assert.Equal(canArchive, viewModel.CanArchiveSelectedPlan);
    }

    [Fact]
    public async Task AddDraftCreatesAndSelectsEditableDraft()
    {
        var service = new FakePlanCatalogService();
        var viewModel = new PlanCatalogViewModel(service);

        await viewModel.AddDraftPlanCommand.ExecuteAsync();

        Assert.NotNull(viewModel.SelectedPlan);
        Assert.Equal(PlanCatalogState.Draft, viewModel.SelectedPlan.State);
        Assert.True(viewModel.CanEditSelectedPlanFields);
        Assert.True(viewModel.CanDeleteSelectedPlan);
        Assert.Equal("New plan", service.CreatedDraftInput?.Name);
    }

    [Fact]
    public async Task SaveDraftPersistsEditedDraftFields()
    {
        var service = new FakePlanCatalogService();
        var draft = service.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        var viewModel = new PlanCatalogViewModel(service);
        await viewModel.LoadAsync();
        viewModel.Name = "Updated";
        viewModel.PriceWithoutVat = 500m;

        await viewModel.SaveDraftPlanCommand.ExecuteAsync();

        Assert.Equal(draft.Id, service.UpdatedPlanId);
        Assert.Equal("Updated", service.UpdatedDraftInput?.Name);
        Assert.Equal(500m, service.UpdatedDraftInput?.PriceWithoutVat);
    }

    [Fact]
    public async Task DeleteDraftRemovesSelectedDraft()
    {
        var service = new FakePlanCatalogService();
        service.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        var viewModel = new PlanCatalogViewModel(service);
        await viewModel.LoadAsync();

        await viewModel.DeleteDraftPlanCommand.ExecuteAsync();

        Assert.True(service.DeleteWasCalled);
        Assert.Empty(viewModel.Plans);
    }

    [Fact]
    public async Task PublishAndArchiveReloadSelectedPlanState()
    {
        var service = new FakePlanCatalogService();
        service.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var viewModel = new PlanCatalogViewModel(service);
        await viewModel.LoadAsync();

        await viewModel.ArchivePlanCommand.ExecuteAsync();
        Assert.Equal(PlanCatalogState.Archived, viewModel.SelectedPlan?.State);
        Assert.True(viewModel.CanPublishSelectedPlan);

        await viewModel.PublishPlanCommand.ExecuteAsync();
        Assert.Equal(PlanCatalogState.Active, viewModel.SelectedPlan?.State);
        Assert.True(viewModel.CanArchiveSelectedPlan);
    }

    [Fact]
    public void PlanCatalogViewInitializesWithStateAwareControls()
    {
        RunOnStaThread(() =>
        {
            var viewModel = new PlanCatalogViewModel(new FakePlanCatalogService());
            var window = new PlanCatalogView(viewModel);

            Assert.Equal("Manage Plans", window.Title);
            Assert.Same(viewModel, window.DataContext);
            Assert.NotNull(window.Content);

            window.Close();
        });
    }

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

    private sealed class FakePlanCatalogService : IPlanCatalogService
    {
        private readonly List<PlanCatalogItem> plans = [];
        private int nextId = 1;

        public PlanDraftInput? CreatedDraftInput { get; private set; }

        public int? UpdatedPlanId { get; private set; }

        public PlanDraftInput? UpdatedDraftInput { get; private set; }

        public bool DeleteWasCalled { get; private set; }

        public PlanCatalogItem Seed(PlanCatalogItem plan)
        {
            var saved = plan with { Id = nextId++ };
            plans.Add(saved);
            return saved;
        }

        public Task<IReadOnlyList<PlanCatalogItem>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlanCatalogItem>>(plans.ToArray());

        public Task<IReadOnlyList<PlanCatalogItem>> ListAssignableAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlanCatalogItem>>(plans.Where(plan => plan.CanBeAssigned).ToArray());

        public Task<OperationResult<PlanCatalogItem>> CreateDraftAsync(PlanDraftInput input, CancellationToken cancellationToken)
        {
            CreatedDraftInput = input;
            return Task.FromResult(OperationResult<PlanCatalogItem>.Success(Seed(CreatePlan(input.Name, PlanCatalogState.Draft))));
        }

        public Task<OperationResult<PlanCatalogItem>> UpdateDraftAsync(int planId, PlanDraftInput input, CancellationToken cancellationToken)
        {
            UpdatedPlanId = planId;
            UpdatedDraftInput = input;
            var index = plans.FindIndex(plan => plan.Id == planId);
            plans[index] = plans[index] with
            {
                Name = input.Name,
                PriceWithoutVat = input.PriceWithoutVat,
                LessonsPerWeek = input.LessonsPerWeek,
                DurationMinutes = input.DurationMinutes,
                Flexibility = input.Flexibility,
                NumberOfStudents = input.NumberOfStudents,
                EffectiveFrom = input.EffectiveFrom
            };
            return Task.FromResult(OperationResult<PlanCatalogItem>.Success(plans[index]));
        }

        public Task<OperationResult<bool>> DeleteDraftAsync(int planId, CancellationToken cancellationToken)
        {
            DeleteWasCalled = true;
            plans.RemoveAll(plan => plan.Id == planId);
            return Task.FromResult(OperationResult<bool>.Success(true));
        }

        public Task<OperationResult<PlanCatalogItem>> PublishAsync(int planId, CancellationToken cancellationToken) =>
            ChangeStateAsync(planId, PlanCatalogState.Active);

        public Task<OperationResult<PlanCatalogItem>> ArchiveAsync(int planId, CancellationToken cancellationToken) =>
            ChangeStateAsync(planId, PlanCatalogState.Archived);

        private Task<OperationResult<PlanCatalogItem>> ChangeStateAsync(int planId, PlanCatalogState state)
        {
            var index = plans.FindIndex(plan => plan.Id == planId);
            plans[index] = plans[index] with { State = state };
            return Task.FromResult(OperationResult<PlanCatalogItem>.Success(plans[index]));
        }
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw exception;
        }
    }
}
