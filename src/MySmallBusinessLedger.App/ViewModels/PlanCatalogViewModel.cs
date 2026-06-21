using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.UseCases.Plans;

namespace MySmallBusinessLedger.App.ViewModels;

public sealed class PlanCatalogViewModel : ViewModelBase
{
    private readonly IPlanCatalogService planCatalogService;
    private readonly IPlanCatalogInteractionService interactionService;
    private PlanCatalogDisplayModel? selectedPlan;
    private string name = string.Empty;
    private decimal? priceWithoutVat;
    private LessonsPerWeek lessonsPerWeek = LessonsPerWeek.One;
    private int durationMinutes = 60;
    private PlanFlexibility flexibility = PlanFlexibility.MoveOrCancelByAgreement;
    private int numberOfStudents = 1;
    private DateTime effectiveDate = DefaultEffectiveDate();
    private string paymentTerms = PlanPaymentTerms.PaidMonthlyInAdvance;
    private DateTime? retireOnDate;
    private string? errorMessage;

    public PlanCatalogViewModel(IPlanCatalogService planCatalogService)
        : this(planCatalogService, new DiscardPlanCatalogInteractionService())
    {
    }

    public PlanCatalogViewModel(
        IPlanCatalogService planCatalogService,
        IPlanCatalogInteractionService interactionService)
    {
        this.planCatalogService = planCatalogService;
        this.interactionService = interactionService;
        PlansView = CollectionViewSource.GetDefaultView(Plans);
        PlansView.GroupDescriptions?.Add(new PropertyGroupDescription(nameof(PlanCatalogDisplayModel.StateText)));

        AddDraftPlanCommand = new AsyncRelayCommand(AddDraftPlanAsync);
        SaveDraftPlanCommand = new AsyncRelayCommand(SaveDraftPlanAsync, () => CanEditSelectedPlanFields);
        SaveNameCommand = new AsyncRelayCommand(SaveNameAsync, () => CanSaveSelectedPlanName);
        SaveSelectedPlanCommand = new AsyncRelayCommand(SaveSelectedPlanAsync, () => CanSaveSelectedPlan);
        DeleteDraftPlanCommand = new AsyncRelayCommand(DeleteDraftPlanAsync, () => CanDeleteSelectedPlan);
        PublishDraftPlanCommand = new AsyncRelayCommand(PublishDraftPlanAsync, () => CanPublishDraftSelectedPlan);
        SaveRetireOnCommand = new AsyncRelayCommand(SaveRetireOnAsync, () => CanSaveSelectedPlanRetireOn);
        RetirePlanCommand = new AsyncRelayCommand(RetirePlanAsync, () => CanRetireSelectedPlan);
        ReactivatePlanCommand = new AsyncRelayCommand(ReactivatePlanAsync, () => CanReactivateSelectedPlan);
    }

    public ObservableCollection<PlanCatalogDisplayModel> Plans { get; } = [];

    public ICollectionView PlansView { get; }

    public IReadOnlyList<DisplayOption<LessonsPerWeek>> LessonsPerWeekOptions { get; } =
        [
            new(LessonsPerWeek.One, "One lesson per week"),
            new(LessonsPerWeek.Two, "Two lessons per week"),
            new(LessonsPerWeek.ByAppointment, "By appointment")
        ];

    public IReadOnlyList<DisplayOption<PlanFlexibility>> FlexibilityOptions { get; } =
        [
            new(PlanFlexibility.None, "No flexibility"),
            new(PlanFlexibility.MoveOrCancelByAgreement, "Move or cancel by agreement")
        ];

    public IReadOnlyList<string> PaymentTermOptions => PlanPaymentTerms.All;

    public PlanCatalogDisplayModel? SelectedPlan
    {
        get => selectedPlan;
        set
        {
            if (SetProperty(ref selectedPlan, value))
            {
                ApplySelectedPlan();
            }
        }
    }

    public string Name { get => name; set => SetProperty(ref name, value); }

    public decimal? PriceWithoutVat { get => priceWithoutVat; set => SetProperty(ref priceWithoutVat, value); }

    public LessonsPerWeek LessonsPerWeek { get => lessonsPerWeek; set => SetProperty(ref lessonsPerWeek, value); }

    public int DurationMinutes { get => durationMinutes; set => SetProperty(ref durationMinutes, value); }

    public PlanFlexibility Flexibility { get => flexibility; set => SetProperty(ref flexibility, value); }

    public int NumberOfStudents { get => numberOfStudents; set => SetProperty(ref numberOfStudents, value); }

    public DateTime EffectiveDate { get => effectiveDate; set => SetProperty(ref effectiveDate, value); }

    public string PaymentTerms { get => paymentTerms; set => SetProperty(ref paymentTerms, value); }

    public DateTime? RetireOnDate { get => retireOnDate; set => SetProperty(ref retireOnDate, value); }

    public bool CanEditSelectedPlanFields => SelectedPlan?.State == PlanCatalogState.Draft;

    public bool CanEditSelectedPlanName => SelectedPlan is not null;

    public bool CanSaveSelectedPlanName => SelectedPlan is not null && SelectedPlan.State != PlanCatalogState.Draft;

    public bool CanEditSelectedPlanRetireOn => SelectedPlan?.State is PlanCatalogState.Draft or PlanCatalogState.Active;

    public bool CanDeleteSelectedPlan => SelectedPlan?.State == PlanCatalogState.Draft;

    public bool CanPublishDraftSelectedPlan => SelectedPlan?.State == PlanCatalogState.Draft;

    public bool CanSaveSelectedPlanRetireOn => SelectedPlan?.State == PlanCatalogState.Active;

    public bool CanRetireSelectedPlan => SelectedPlan?.State == PlanCatalogState.Active;

    public bool CanReactivateSelectedPlan => SelectedPlan?.State == PlanCatalogState.Retired;

    public bool CanSaveSelectedPlan => SelectedPlan is not null && SelectedPlan.State != PlanCatalogState.Draft;

    public string? ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public AsyncRelayCommand AddDraftPlanCommand { get; }

    public AsyncRelayCommand SaveDraftPlanCommand { get; }

    public AsyncRelayCommand SaveNameCommand { get; }

    public AsyncRelayCommand SaveSelectedPlanCommand { get; }

    public AsyncRelayCommand DeleteDraftPlanCommand { get; }

    public AsyncRelayCommand PublishDraftPlanCommand { get; }

    public AsyncRelayCommand SaveRetireOnCommand { get; }

    public AsyncRelayCommand RetirePlanCommand { get; }

    public AsyncRelayCommand ReactivatePlanCommand { get; }

    public Task LoadAsync() => ReloadAsync();

    private async Task AddDraftPlanAsync()
    {
        if (!await PrepareCurrentPlanForAddAsync())
        {
            return;
        }

        var result = await planCatalogService.CreateDraftAsync(
            new PlanDraftInput(
                string.Empty,
                null,
                LessonsPerWeek.One,
                DurationMinutes: 60,
                PlanFlexibility.MoveOrCancelByAgreement,
                NumberOfStudents: 1,
                new DateTimeOffset(DefaultEffectiveDate(), TimeSpan.Zero),
                PaymentTerms: PlanPaymentTerms.PaidMonthlyInAdvance,
                RetireOn: null),
            CancellationToken.None);
        if (!result.Succeeded)
        {
            SetError(result.Errors.FirstOrDefault()?.Message ?? "Plan could not be created.");
            return;
        }

        ErrorMessage = null;
        await ReloadAsync(result.Value!.Id);
    }

    private async Task SaveDraftPlanAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        await SaveDraftPlanCoreAsync(SelectedPlan.Id);
    }

    private async Task SaveNameAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var result = await planCatalogService.RenameAsync(
            SelectedPlan.Id,
            Name,
            CancellationToken.None);
        if (!result.Succeeded)
        {
            SetError(result.Errors.FirstOrDefault()?.Message ?? "Plan name could not be saved.");
            return;
        }

        ErrorMessage = null;
        await ReloadAsync(result.Value!.Id);
    }

    private async Task SaveSelectedPlanAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        await SaveSelectedPlanCoreAsync(SelectedPlan.Id);
    }

    private async Task<bool> SaveSelectedPlanCoreAsync(int planId)
    {
        if (SelectedPlan is null)
        {
            return true;
        }

        var selectedPlan = SelectedPlan;
        if (!string.Equals(Name, selectedPlan.Name, StringComparison.Ordinal))
        {
            var renameResult = await planCatalogService.RenameAsync(planId, Name, CancellationToken.None);
            if (!renameResult.Succeeded)
            {
                SetError(renameResult.Errors.FirstOrDefault()?.Message ?? "Plan name could not be saved.");
                return false;
            }
        }

        if (selectedPlan.State == PlanCatalogState.Active && HasRetireOnChanged(selectedPlan.Plan))
        {
            var retireOnResult = await planCatalogService.UpdateRetireOnAsync(
                planId,
                ToRetireOn(),
                CancellationToken.None);
            if (!retireOnResult.Succeeded)
            {
                SetError(retireOnResult.Errors.FirstOrDefault()?.Message ?? "Retire on could not be saved.");
                return false;
            }
        }

        ErrorMessage = null;
        await ReloadAsync(planId);
        return true;
    }

    private async Task<bool> SaveDraftPlanCoreAsync(int planId)
    {
        if (SelectedPlan is null)
        {
            return true;
        }

        var result = await planCatalogService.UpdateDraftAsync(
            planId,
            CreateDraftInput(),
            CancellationToken.None);
        if (!result.Succeeded)
        {
            SetError(result.Errors.FirstOrDefault()?.Message ?? "Plan could not be saved.");
            return false;
        }

        ErrorMessage = null;
        await ReloadAsync(result.Value!.Id);
        return true;
    }

    private async Task DeleteDraftPlanAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var result = await planCatalogService.DeleteDraftAsync(SelectedPlan.Id, CancellationToken.None);
        if (!result.Succeeded)
        {
            SetError(result.Errors.FirstOrDefault()?.Message ?? "Plan could not be deleted.");
            return;
        }

        ErrorMessage = null;
        await ReloadAsync();
    }

    private async Task PublishDraftPlanAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var result = await planCatalogService.PublishDraftAsync(SelectedPlan.Id, CancellationToken.None);
        if (!result.Succeeded)
        {
            SetError(result.Errors.FirstOrDefault()?.Message ?? "Draft plan could not be published.");
            return;
        }

        ErrorMessage = null;
        await ReloadAsync(result.Value!.Id);
    }

    private async Task SaveRetireOnAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        await SaveRetireOnCoreAsync(SelectedPlan.Id);
    }

    private async Task<bool> SaveRetireOnCoreAsync(int planId)
    {
        if (SelectedPlan is null)
        {
            return true;
        }

        var result = await planCatalogService.UpdateRetireOnAsync(
            planId,
            ToRetireOn(),
            CancellationToken.None);
        if (!result.Succeeded)
        {
            SetError(result.Errors.FirstOrDefault()?.Message ?? "Retire on could not be saved.");
            return false;
        }

        ErrorMessage = null;
        await ReloadAsync(result.Value!.Id);
        return true;
    }

    private async Task RetirePlanAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var result = await planCatalogService.RetireAsync(SelectedPlan.Id, CancellationToken.None);
        if (!result.Succeeded)
        {
            SetError(result.Errors.FirstOrDefault()?.Message ?? "Plan could not be retired.");
            return;
        }

        ErrorMessage = null;
        await ReloadAsync(result.Value!.Id);
    }

    private async Task ReactivatePlanAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var result = await planCatalogService.ReactivateAsync(SelectedPlan.Id, CancellationToken.None);
        if (!result.Succeeded)
        {
            SetError(result.Errors.FirstOrDefault()?.Message ?? "Plan could not be reactivated.");
            return;
        }

        ErrorMessage = null;
        await ReloadAsync(result.Value!.Id);
    }

    private async Task ReloadAsync(int? selectedPlanId = null)
    {
        var plans = await planCatalogService.ListAsync(CancellationToken.None);
        Plans.Clear();
        foreach (var plan in plans
            .OrderBy(plan => StateSortOrder(plan.State))
            .ThenBy(plan => plan.Name, StringComparer.CurrentCulture))
        {
            Plans.Add(new PlanCatalogDisplayModel(plan));
        }

        PlansView.Refresh();
        SelectedPlan = selectedPlanId is null
            ? Plans.FirstOrDefault()
            : Plans.FirstOrDefault(plan => plan.Id == selectedPlanId.Value) ?? Plans.FirstOrDefault();
    }

    private void ApplySelectedPlan()
    {
        if (SelectedPlan is { Plan: var plan })
        {
            Name = plan.Name;
            PriceWithoutVat = plan.PriceWithoutVat;
            LessonsPerWeek = plan.LessonsPerWeek;
            DurationMinutes = plan.DurationMinutes;
            Flexibility = plan.Flexibility;
            NumberOfStudents = plan.NumberOfStudents;
            EffectiveDate = plan.EffectiveFrom.Date;
            PaymentTerms = plan.PaymentTerms;
            RetireOnDate = plan.RetireOn?.Date;
        }

        OnPropertyChanged(nameof(CanEditSelectedPlanFields));
        OnPropertyChanged(nameof(CanEditSelectedPlanName));
        OnPropertyChanged(nameof(CanSaveSelectedPlanName));
        OnPropertyChanged(nameof(CanEditSelectedPlanRetireOn));
        OnPropertyChanged(nameof(CanDeleteSelectedPlan));
        OnPropertyChanged(nameof(CanPublishDraftSelectedPlan));
        OnPropertyChanged(nameof(CanSaveSelectedPlanRetireOn));
        OnPropertyChanged(nameof(CanRetireSelectedPlan));
        OnPropertyChanged(nameof(CanReactivateSelectedPlan));
        OnPropertyChanged(nameof(CanSaveSelectedPlan));
        SaveDraftPlanCommand.RaiseCanExecuteChanged();
        SaveNameCommand.RaiseCanExecuteChanged();
        SaveSelectedPlanCommand.RaiseCanExecuteChanged();
        DeleteDraftPlanCommand.RaiseCanExecuteChanged();
        PublishDraftPlanCommand.RaiseCanExecuteChanged();
        SaveRetireOnCommand.RaiseCanExecuteChanged();
        RetirePlanCommand.RaiseCanExecuteChanged();
        ReactivatePlanCommand.RaiseCanExecuteChanged();
    }

    private PlanDraftInput CreateDraftInput() =>
        new(
            Name,
            PriceWithoutVat,
            LessonsPerWeek,
            DurationMinutes,
            Flexibility,
            NumberOfStudents,
            new DateTimeOffset(EffectiveDate.Date, TimeSpan.Zero),
            PaymentTerms,
            ToRetireOn());

    private DateTimeOffset? ToRetireOn() =>
        RetireOnDate is null ? null : new DateTimeOffset(RetireOnDate.Value.Date, TimeSpan.Zero);

    private async Task<bool> PrepareCurrentPlanForAddAsync()
    {
        if (SelectedPlan is null)
        {
            return true;
        }

        if (SelectedPlan.State == PlanCatalogState.Draft && HasDraftChanges(SelectedPlan.Plan))
        {
            return await SaveDraftPlanCoreAsync(SelectedPlan.Id);
        }

        if (SelectedPlan.State != PlanCatalogState.Draft && HasSelectedPlanChanges(SelectedPlan))
        {
            var choice = await interactionService.PromptAddPlanSaveChangesAsync(CancellationToken.None);
            return choice switch
            {
                PlanCatalogAddPlanChoice.Save => await SaveSelectedPlanCoreAsync(SelectedPlan.Id),
                PlanCatalogAddPlanChoice.Discard => true,
                _ => false
            };
        }

        return true;
    }

    private bool HasDraftChanges(PlanCatalogItem plan) =>
        !string.Equals(Name, plan.Name, StringComparison.Ordinal) ||
        PriceWithoutVat != plan.PriceWithoutVat ||
        LessonsPerWeek != plan.LessonsPerWeek ||
        DurationMinutes != plan.DurationMinutes ||
        Flexibility != plan.Flexibility ||
        NumberOfStudents != plan.NumberOfStudents ||
        new DateTimeOffset(EffectiveDate.Date, TimeSpan.Zero) != plan.EffectiveFrom ||
        !string.Equals(PaymentTerms, plan.PaymentTerms, StringComparison.Ordinal) ||
        ToRetireOn() != plan.RetireOn;

    private bool HasRetireOnChanged(PlanCatalogItem plan) =>
        ToRetireOn() != plan.RetireOn;

    private bool HasSelectedPlanChanges(PlanCatalogDisplayModel plan) =>
        !string.Equals(Name, plan.Name, StringComparison.Ordinal) ||
        plan.State == PlanCatalogState.Active && HasRetireOnChanged(plan.Plan);

    private void SetError(string message)
    {
        ErrorMessage = message;
    }

    private static int StateSortOrder(PlanCatalogState state) =>
        state switch
        {
            PlanCatalogState.Draft => 0,
            PlanCatalogState.Active => 1,
            PlanCatalogState.Retired => 2,
            _ => 3
        };

    private static DateTime DefaultEffectiveDate()
    {
        var today = DateTime.Today;
        return new DateTime(today.Year, today.Month, 1);
    }
}
