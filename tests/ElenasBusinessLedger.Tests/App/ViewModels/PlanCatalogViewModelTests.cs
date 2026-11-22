using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.App.Views;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.UseCases.Plans;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfGrid = System.Windows.Controls.Grid;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace ElenasBusinessLedger.Tests.App.ViewModels;

// Checks plan editor state, draft saves, lifecycle commands, and prompts about unsaved changes.
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
    [InlineData(PlanCatalogState.Draft, true, true, true, true, false, false, false, false)]
    [InlineData(PlanCatalogState.Active, false, true, false, false, true, true, false, true)]
    [InlineData(PlanCatalogState.Retired, false, false, false, false, false, false, true, true)]
    public async Task SelectedPlanExposesOnlyStateRelevantEditingAndActions(
        PlanCatalogState state,
        bool canEditFields,
        bool canEditRetireOn,
        bool canDelete,
        bool canPublishDraft,
        bool canSaveRetireOn,
        bool canRetire,
        bool canReactivate,
        bool canSaveSelectedPlan)
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
        Assert.Equal(canSaveSelectedPlan, viewModel.CanSaveSelectedPlan);
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
        Assert.Equal(string.Empty, service.CreatedDraftInput?.Name);
        Assert.Null(service.CreatedDraftInput?.PriceWithoutVat);
        Assert.Equal(LessonsPerWeek.One, service.CreatedDraftInput?.LessonsPerWeek);
        Assert.Equal(60, service.CreatedDraftInput?.DurationMinutes);
        Assert.Equal(1, service.CreatedDraftInput?.NumberOfStudents);
        Assert.Equal(PlanFlexibility.MoveOrCancelByAgreement, service.CreatedDraftInput?.Flexibility);
        Assert.Equal("Paid monthly in advance", service.CreatedDraftInput?.PaymentTerms);
        Assert.Equal(new DateTimeOffset(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), TimeSpan.Zero), service.CreatedDraftInput?.EffectiveFrom);
        Assert.Null(service.CreatedDraftInput?.RetireOn);
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
        viewModel.PaymentTerms = "Paid monthly in advance";
        viewModel.RetireOnDate = new DateTime(2026, 7, 1);

        await viewModel.SaveDraftPlanCommand.ExecuteAsync();

        Assert.Equal(draft.Id, service.UpdatedPlanId);
        Assert.Equal("Updated", service.UpdatedDraftInput?.Name);
        Assert.Equal(500m, service.UpdatedDraftInput?.PriceWithoutVat);
        Assert.Equal("Paid monthly in advance", service.UpdatedDraftInput?.PaymentTerms);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), service.UpdatedDraftInput?.RetireOn);
    }

    [Fact]
    public async Task AddDraftSavesChangedDraftBeforeCreatingNewDraft()
    {
        var service = new FakePlanCatalogService();
        var draft = service.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        var viewModel = new PlanCatalogViewModel(service, new FakePlanCatalogInteractionService());
        await viewModel.LoadAsync();
        viewModel.Name = "Saved before add";
        viewModel.PaymentTerms = "Paid per lesson";

        await viewModel.AddDraftPlanCommand.ExecuteAsync();

        Assert.Equal(draft.Id, service.UpdatedPlanId);
        Assert.Equal("Saved before add", service.UpdatedDraftInput?.Name);
        Assert.Equal("Paid per lesson", service.UpdatedDraftInput?.PaymentTerms);
        Assert.Equal(["UpdateDraft", "CreateDraft"], service.Calls);
        Assert.Equal(string.Empty, viewModel.SelectedPlan?.Name);
    }

    [Fact]
    public async Task AddDraftPromptsBeforeSavingChangedActivePlan()
    {
        var service = new FakePlanCatalogService();
        var active = service.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var interactions = new FakePlanCatalogInteractionService { NextAddPlanChoice = PlanCatalogAddPlanChoice.Save };
        var viewModel = new PlanCatalogViewModel(service, interactions);
        await viewModel.LoadAsync();
        viewModel.Name = "Active renamed before add";
        viewModel.RetireOnDate = new DateTime(2026, 10, 1);

        await viewModel.AddDraftPlanCommand.ExecuteAsync();

        Assert.Equal(1, interactions.PromptAddPlanSaveChangesCount);
        Assert.Equal(active.Id, service.RenamedPlanId);
        Assert.Equal("Active renamed before add", service.RenamedPlanName);
        Assert.Equal(active.Id, service.UpdatedRetireOnPlanId);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), service.UpdatedRetireOn);
        Assert.Equal(["Rename", "UpdateRetireOn", "CreateDraft"], service.Calls);
        Assert.Equal(string.Empty, viewModel.SelectedPlan?.Name);
    }

    [Fact]
    public async Task AddDraftCanCancelWhenChangedActivePlanIsPrompted()
    {
        var service = new FakePlanCatalogService();
        service.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var interactions = new FakePlanCatalogInteractionService { NextAddPlanChoice = PlanCatalogAddPlanChoice.Cancel };
        var viewModel = new PlanCatalogViewModel(service, interactions);
        await viewModel.LoadAsync();
        viewModel.RetireOnDate = new DateTime(2026, 10, 1);

        await viewModel.AddDraftPlanCommand.ExecuteAsync();

        Assert.Equal(1, interactions.PromptAddPlanSaveChangesCount);
        Assert.Null(service.CreatedDraftInput);
        Assert.Null(service.UpdatedRetireOnPlanId);
        Assert.Equal("Active", viewModel.SelectedPlan?.Name);
    }

    [Fact]
    public async Task PrepareForCloseCanCancelWhenDraftHasUnsavedChanges()
    {
        var service = new FakePlanCatalogService();
        service.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        var interactions = new FakePlanCatalogInteractionService { NextClosePlanChoice = PlanCatalogAddPlanChoice.Cancel };
        var viewModel = new PlanCatalogViewModel(service, interactions);
        await viewModel.LoadAsync();
        viewModel.Name = "Unsaved draft";

        var canClose = await viewModel.PrepareForCloseAsync();

        Assert.False(canClose);
        Assert.Equal(1, interactions.PromptClosePlanSaveChangesCount);
        Assert.Null(service.UpdatedDraftInput);
        Assert.Equal("Draft", viewModel.SelectedPlan?.Name);
    }

    [Fact]
    public async Task PrepareForCloseSavesChangedDraftWhenRequested()
    {
        var service = new FakePlanCatalogService();
        var draft = service.Seed(CreatePlan("Draft", PlanCatalogState.Draft));
        var interactions = new FakePlanCatalogInteractionService { NextClosePlanChoice = PlanCatalogAddPlanChoice.Save };
        var viewModel = new PlanCatalogViewModel(service, interactions);
        await viewModel.LoadAsync();
        viewModel.Name = "Saved draft";

        var canClose = await viewModel.PrepareForCloseAsync();

        Assert.True(canClose);
        Assert.Equal(1, interactions.PromptClosePlanSaveChangesCount);
        Assert.Equal(draft.Id, service.UpdatedPlanId);
        Assert.Equal("Saved draft", service.UpdatedDraftInput?.Name);
    }

    [Fact]
    public async Task PrepareForClosePromptsBeforeSavingChangedActivePlan()
    {
        var service = new FakePlanCatalogService();
        var active = service.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var interactions = new FakePlanCatalogInteractionService { NextClosePlanChoice = PlanCatalogAddPlanChoice.Save };
        var viewModel = new PlanCatalogViewModel(service, interactions);
        await viewModel.LoadAsync();
        viewModel.Name = "Active close rename";
        viewModel.RetireOnDate = new DateTime(2026, 10, 1);

        var canClose = await viewModel.PrepareForCloseAsync();

        Assert.True(canClose);
        Assert.Equal(1, interactions.PromptClosePlanSaveChangesCount);
        Assert.Equal(active.Id, service.RenamedPlanId);
        Assert.Equal("Active close rename", service.RenamedPlanName);
        Assert.Equal(active.Id, service.UpdatedRetireOnPlanId);
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
    public async Task SaveActivePlanUpdatesNameAndRetireDateWithoutEditingImmutableFields()
    {
        var service = new FakePlanCatalogService();
        var active = service.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var viewModel = new PlanCatalogViewModel(service);
        await viewModel.LoadAsync();
        viewModel.Name = "Active renamed";
        viewModel.RetireOnDate = new DateTime(2026, 9, 15);

        await viewModel.SaveSelectedPlanCommand.ExecuteAsync();

        Assert.Equal(active.Id, service.RenamedPlanId);
        Assert.Equal("Active renamed", service.RenamedPlanName);
        Assert.Equal(active.Id, service.UpdatedRetireOnPlanId);
        Assert.Equal(new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero), service.UpdatedRetireOn);
        Assert.Null(service.UpdatedDraftInput);
        Assert.Equal("Active renamed", viewModel.SelectedPlan?.Name);
        Assert.Equal(new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero), viewModel.SelectedPlan?.Plan.RetireOn);
    }

    [Fact]
    public async Task SaveNameRenamesActivePlanWithoutEditingImmutableFields()
    {
        var service = new FakePlanCatalogService();
        var active = service.Seed(CreatePlan("Active", PlanCatalogState.Active));
        var viewModel = new PlanCatalogViewModel(service);
        await viewModel.LoadAsync();
        viewModel.Name = "Active renamed";
        viewModel.PriceWithoutVat = 999m;

        await viewModel.SaveNameCommand.ExecuteAsync();

        Assert.Equal(active.Id, service.RenamedPlanId);
        Assert.Equal("Active renamed", service.RenamedPlanName);
        Assert.Null(service.UpdatedDraftInput);
        Assert.Equal("Active renamed", viewModel.SelectedPlan?.Name);
        Assert.Equal(400m, viewModel.SelectedPlan?.Plan.PriceWithoutVat);
    }

    [Fact]
    public void PlanCatalogViewInitializesWithStateAwareControls()
    {
        WpfTestThread.Run(() =>
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
            Assert.Equal(addPlanButton.Height, closeButton.Height);

            var saveButton = Assert.Single(FindChildren<WpfButton>(window), button => button.Content?.ToString() == "Save");
            Assert.Equal(WpfGrid.GetRow(addPlanButton), WpfGrid.GetRow(saveButton));
            Assert.Equal(2, WpfGrid.GetColumn(saveButton));

            var retireButton = Assert.Single(FindChildren<WpfButton>(window), button => button.Content?.ToString() == "Retire");
            Assert.Equal(HorizontalAlignment.Center, retireButton.HorizontalAlignment);
            Assert.Equal(2, WpfGrid.GetColumnSpan(retireButton));

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

            var paymentTermsComboBox = Assert.Single(comboBoxes, comboBox =>
                comboBox.GetBindingExpression(WpfComboBox.SelectedItemProperty)?.ParentBinding.Path.Path == nameof(PlanCatalogViewModel.PaymentTerms));
            Assert.Empty(paymentTermsComboBox.DisplayMemberPath);

            var textBoxes = FindChildren<WpfTextBox>(window);
            var fieldOrder = textBoxes
                .Select(textBox => textBox.GetBindingExpression(WpfTextBox.TextProperty)?.ParentBinding.Path.Path)
                .Where(path => path is not null)
                .Select(path => path!)
                .ToArray();
            Assert.Equal(
                [
                    nameof(PlanCatalogViewModel.Name),
                    nameof(PlanCatalogViewModel.PriceWithoutVat),
                    nameof(PlanCatalogViewModel.DurationMinutes),
                    nameof(PlanCatalogViewModel.NumberOfStudents)
                ],
                fieldOrder);

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
        Assert.Equal(
            ["Paid monthly in advance", "Paid per lesson"],
            viewModel.PaymentTermOptions);
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
            PaymentTerms: "Paid monthly in advance",
            RetireOn: null,
            state);

    // Provides an in-memory catalog and records edits so tests can assert the view model's command order.
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

        public int? RenamedPlanId { get; private set; }

        public string? RenamedPlanName { get; private set; }

        public List<string> Calls { get; } = [];

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
            Calls.Add("CreateDraft");
            CreatedDraftInput = input;
            return Task.FromResult(OperationResult<PlanCatalogItem>.Success(Seed(CreatePlan(input.Name, PlanCatalogState.Draft) with
            {
                PaymentTerms = input.PaymentTerms
            })));
        }

        public Task<OperationResult<PlanCatalogItem>> UpdateDraftAsync(int planId, PlanDraftInput input, CancellationToken cancellationToken)
        {
            Calls.Add("UpdateDraft");
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
                PaymentTerms = input.PaymentTerms,
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
            Calls.Add("UpdateRetireOn");
            UpdatedRetireOnPlanId = planId;
            UpdatedRetireOn = retireOn;
            var index = plans.FindIndex(plan => plan.Id == planId);
            plans[index] = plans[index] with { RetireOn = retireOn };
            return Task.FromResult(OperationResult<PlanCatalogItem>.Success(plans[index]));
        }

        public Task<OperationResult<PlanCatalogItem>> RenameAsync(
            int planId,
            string newName,
            CancellationToken cancellationToken)
        {
            Calls.Add("Rename");
            RenamedPlanId = planId;
            RenamedPlanName = newName;
            var index = plans.FindIndex(plan => plan.Id == planId);
            plans[index] = plans[index] with { Name = newName };
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

    // Returns scripted decisions for unsaved plan changes and records when the editor asks.
    private sealed class FakePlanCatalogInteractionService : IPlanCatalogInteractionService
    {
        public PlanCatalogAddPlanChoice NextAddPlanChoice { get; init; } = PlanCatalogAddPlanChoice.Discard;

        public PlanCatalogAddPlanChoice NextClosePlanChoice { get; set; } = PlanCatalogAddPlanChoice.Discard;

        public int PromptAddPlanSaveChangesCount { get; private set; }

        public int PromptClosePlanSaveChangesCount { get; private set; }

        public Task<PlanCatalogAddPlanChoice> PromptAddPlanSaveChangesAsync(CancellationToken cancellationToken)
        {
            PromptAddPlanSaveChangesCount++;
            return Task.FromResult(NextAddPlanChoice);
        }

        public Task<PlanCatalogAddPlanChoice> PromptClosePlanSaveChangesAsync(CancellationToken cancellationToken)
        {
            PromptClosePlanSaveChangesCount++;
            return Task.FromResult(NextClosePlanChoice);
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
