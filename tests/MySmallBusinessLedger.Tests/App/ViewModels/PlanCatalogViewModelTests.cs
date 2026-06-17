using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.UseCases.Plans;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfGrid = System.Windows.Controls.Grid;

namespace MySmallBusinessLedger.Tests.App.ViewModels;

public sealed class PlanCatalogViewModelTests
{
    [Fact]
    public async Task LoadListsAllPlansByState()
    {
        var service = new FakePlanCatalogService();
        service.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        service.Seed(CreatePlan("Active", PlanCatalogState.Active));
        service.Seed(CreatePlan("Retired", PlanCatalogState.Retired));
        var viewModel = new PlanCatalogViewModel(service);

        await viewModel.LoadAsync();

        Assert.Equal(3, viewModel.Plans.Count);
        Assert.Equal(["Draft", "Active", "Retired"], viewModel.Plans.Select(plan => plan.StateText).Distinct().ToArray());
        Assert.Equal("Draft", viewModel.SelectedPlan?.Name);
    }

    [Theory]
    [InlineData(PlanCatalogState.Draft, true, true, true, true, false, false, false)]
    [InlineData(PlanCatalogState.Active, false, true, false, false, true, true, false)]
    [InlineData(PlanCatalogState.Retired, false, false, false, false, false, false, true)]
    public async Task SelectedPlanExposesOnlyStateRelevantEditingAndActions(
        PlanCatalogState state,
        bool canEditFields,
        bool canEditRetireOn,
        bool canDelete,
        bool canPublishDraft,
        bool canSaveRetireOn,
        bool canRetire,
        bool canReactivate)
    {
        var service = new FakePlanCatalogService();
        service.Seed(CreatePlan("Selected", state));
        var viewModel = new PlanCatalogViewModel(service);

        await viewModel.LoadAsync();

        Assert.Equal(canEditFields, viewModel.CanEditSelectedPlanFields);
        Assert.Equal(canEditRetireOn, viewModel.CanEditSelectedPlanRetireOn);
        Assert.Equal(canDelete, viewModel.CanDeleteSelectedPlan);
        Assert.Equal(canPublishDraft, viewModel.CanPublishDraftSelectedPlan);
        Assert.Equal(canSaveRetireOn, viewModel.CanSaveSelectedPlanRetireOn);
        Assert.Equal(canRetire, viewModel.CanRetireSelectedPlan);
        Assert.Equal(canReactivate, viewModel.CanReactivateSelectedPlan);
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
        Assert.True(viewModel.CanEditSelectedPlanRetireOn);
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
        viewModel.RetireOnDate = new DateTime(2026, 7, 1);

        await viewModel.SaveDraftPlanCommand.ExecuteAsync();

        Assert.Equal(draft.Id, service.UpdatedPlanId);
        Assert.Equal("Updated", service.UpdatedDraftInput?.Name);
        Assert.Equal(500m, service.UpdatedDraftInput?.PriceWithoutVat);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), service.UpdatedDraftInput?.RetireOn);
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
    public async Task RetireAndReactivateReloadSelectedPlanState()
    {
        var service = new FakePlanCatalogService();
        service.Seed(CreatePlan("Active", PlanCatalogState.Active) with
        {
            RetireOn = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)
        });
        var viewModel = new PlanCatalogViewModel(service);
        await viewModel.LoadAsync();

        await viewModel.RetirePlanCommand.ExecuteAsync();
        Assert.Equal(PlanCatalogState.Retired, viewModel.SelectedPlan?.State);
        Assert.True(viewModel.CanReactivateSelectedPlan);

        await viewModel.ReactivatePlanCommand.ExecuteAsync();
        Assert.Equal(PlanCatalogState.Active, viewModel.SelectedPlan?.State);
        Assert.True(viewModel.CanRetireSelectedPlan);
        Assert.NotNull(viewModel.SelectedPlan);
        Assert.Null(viewModel.SelectedPlan.Plan.RetireOn);
    }

    [Fact]
    public async Task SaveRetireOnUpdatesActivePlanWithoutEditingImmutableFields()
    {
        var service = new FakePlanCatalogService();
        var active = service.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var viewModel = new PlanCatalogViewModel(service);
        await viewModel.LoadAsync();
        viewModel.Name = "Should not be saved as a draft";
        viewModel.RetireOnDate = new DateTime(2026, 9, 15);

        await viewModel.SaveRetireOnCommand.ExecuteAsync();

        Assert.Equal(active.Id, service.UpdatedRetireOnPlanId);
        Assert.Equal(new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero), service.UpdatedRetireOn);
        Assert.Null(service.UpdatedDraftInput);
        Assert.Equal("Active", viewModel.SelectedPlan?.Name);
        Assert.Equal(new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero), viewModel.SelectedPlan?.Plan.RetireOn);
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
            var plansGrid = Assert.Single(FindChildren<WpfDataGrid>(window), grid => grid.Name == "PlansGrid");
            Assert.Equal(["Plan", "Price", "Duration", "Lessons", "Students", "State"], plansGrid.Columns.Select(column => column.Header?.ToString() ?? string.Empty).ToArray());

            var addPlanButton = Assert.Single(FindChildren<WpfButton>(window), button => button.Content?.ToString() == "Add Plan");
            Assert.Equal(1, WpfGrid.GetRow(addPlanButton));
            Assert.Equal(0, WpfGrid.GetColumn(addPlanButton));

            var closeButton = Assert.Single(FindChildren<WpfButton>(window), button => button.Content?.ToString() == "Close");
            Assert.Equal(WpfGrid.GetRow(addPlanButton), WpfGrid.GetRow(closeButton));
            Assert.Equal(2, WpfGrid.GetColumn(closeButton));

            var publishDraftButton = Assert.Single(FindChildren<WpfButton>(window), button => button.Content?.ToString() == "Publish draft");
            Assert.Equal(0, WpfGrid.GetRow(publishDraftButton));
            Assert.Equal(1, WpfGrid.GetColumn(publishDraftButton));

            var comboBoxes = FindChildren<WpfComboBox>(window);
            var lessonsComboBox = Assert.Single(comboBoxes, comboBox =>
                comboBox.GetBindingExpression(WpfComboBox.SelectedValueProperty)?.ParentBinding.Path.Path == nameof(PlanCatalogViewModel.LessonsPerWeek));
            Assert.Equal("DisplayName", lessonsComboBox.DisplayMemberPath);
            Assert.Equal("Value", lessonsComboBox.SelectedValuePath);

            var flexibilityComboBox = Assert.Single(comboBoxes, comboBox =>
                comboBox.GetBindingExpression(WpfComboBox.SelectedValueProperty)?.ParentBinding.Path.Path == nameof(PlanCatalogViewModel.Flexibility));
            Assert.Equal("DisplayName", flexibilityComboBox.DisplayMemberPath);
            Assert.Equal("Value", flexibilityComboBox.SelectedValuePath);

            window.Close();
        });
    }

    [Fact]
    public void PlanCatalogOptionsUseHumanReadableLabels()
    {
        var viewModel = new PlanCatalogViewModel(new FakePlanCatalogService());

        Assert.Equal(
            ["One lesson per week", "Two lessons per week", "By appointment"],
            viewModel.LessonsPerWeekOptions.Select(option => option.DisplayName).ToArray());
        Assert.Equal(
            ["No flexibility", "Move or cancel by agreement"],
            viewModel.FlexibilityOptions.Select(option => option.DisplayName).ToArray());
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
            RetireOn: null,
            state);

    private sealed class FakePlanCatalogService : IPlanCatalogService
    {
        private readonly List<PlanCatalogItem> plans = [];
        private int nextId = 1;

        public PlanDraftInput? CreatedDraftInput { get; private set; }

        public int? UpdatedPlanId { get; private set; }

        public PlanDraftInput? UpdatedDraftInput { get; private set; }

        public bool DeleteWasCalled { get; private set; }

        public int? UpdatedRetireOnPlanId { get; private set; }

        public DateTimeOffset? UpdatedRetireOn { get; private set; }

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
                EffectiveFrom = input.EffectiveFrom,
                RetireOn = input.RetireOn
            };
            return Task.FromResult(OperationResult<PlanCatalogItem>.Success(plans[index]));
        }

        public Task<OperationResult<bool>> DeleteDraftAsync(int planId, CancellationToken cancellationToken)
        {
            DeleteWasCalled = true;
            plans.RemoveAll(plan => plan.Id == planId);
            return Task.FromResult(OperationResult<bool>.Success(true));
        }

        public Task<OperationResult<PlanCatalogItem>> PublishDraftAsync(int planId, CancellationToken cancellationToken) =>
            ChangeStateAsync(planId, PlanCatalogState.Active);

        public Task<OperationResult<PlanCatalogItem>> RetireAsync(int planId, CancellationToken cancellationToken) =>
            ChangeStateAsync(planId, PlanCatalogState.Retired);

        public Task<OperationResult<PlanCatalogItem>> ReactivateAsync(int planId, CancellationToken cancellationToken)
        {
            var index = plans.FindIndex(plan => plan.Id == planId);
            plans[index] = plans[index] with { State = PlanCatalogState.Active, RetireOn = null };
            return Task.FromResult(OperationResult<PlanCatalogItem>.Success(plans[index]));
        }

        public Task<OperationResult<PlanCatalogItem>> UpdateRetireOnAsync(
            int planId,
            DateTimeOffset? retireOn,
            CancellationToken cancellationToken)
        {
            UpdatedRetireOnPlanId = planId;
            UpdatedRetireOn = retireOn;
            var index = plans.FindIndex(plan => plan.Id == planId);
            plans[index] = plans[index] with { RetireOn = retireOn };
            return Task.FromResult(OperationResult<PlanCatalogItem>.Success(plans[index]));
        }

        public Task<OperationResult<int>> RetireDueAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<int>.Success(0));

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

    private static IReadOnlyList<T> FindChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        var matches = new List<T>();
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is T typedChild)
            {
                matches.Add(typedChild);
            }

            if (child is DependencyObject dependencyObject)
            {
                matches.AddRange(FindChildren<T>(dependencyObject));
            }
        }

        return matches;
    }
}
