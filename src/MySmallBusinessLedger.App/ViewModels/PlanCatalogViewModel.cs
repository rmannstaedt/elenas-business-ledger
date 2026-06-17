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
    private PlanCatalogDisplayModel? selectedPlan;
    private string name = string.Empty;
    private decimal priceWithoutVat;
    private LessonsPerWeek lessonsPerWeek = LessonsPerWeek.One;
    private int durationMinutes = 60;
    private PlanFlexibility flexibility = PlanFlexibility.MoveOrCancelByAgreement;
    private int numberOfStudents = 1;
    private DateTime effectiveDate = new DateOnly(2026, 1, 1).ToDateTime(TimeOnly.MinValue);
    private string? errorMessage;

    public PlanCatalogViewModel(IPlanCatalogService planCatalogService)
    {
        this.planCatalogService = planCatalogService;
        PlansView = CollectionViewSource.GetDefaultView(Plans);
        PlansView.GroupDescriptions?.Add(new PropertyGroupDescription(nameof(PlanCatalogDisplayModel.StateText)));

        AddDraftPlanCommand = new AsyncRelayCommand(AddDraftPlanAsync);
        SaveDraftPlanCommand = new AsyncRelayCommand(SaveDraftPlanAsync, () => CanEditSelectedPlanFields);
        DeleteDraftPlanCommand = new AsyncRelayCommand(DeleteDraftPlanAsync, () => CanDeleteSelectedPlan);
        PublishPlanCommand = new AsyncRelayCommand(PublishPlanAsync, () => CanPublishSelectedPlan);
        ArchivePlanCommand = new AsyncRelayCommand(ArchivePlanAsync, () => CanArchiveSelectedPlan);
    }

    public ObservableCollection<PlanCatalogDisplayModel> Plans { get; } = [];

    public ICollectionView PlansView { get; }

    public IReadOnlyList<LessonsPerWeek> LessonsPerWeekOptions { get; } =
        [LessonsPerWeek.One, LessonsPerWeek.Two, LessonsPerWeek.ByAppointment];

    public IReadOnlyList<PlanFlexibility> FlexibilityOptions { get; } =
        [PlanFlexibility.None, PlanFlexibility.MoveOrCancelByAgreement];

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

    public decimal PriceWithoutVat { get => priceWithoutVat; set => SetProperty(ref priceWithoutVat, value); }

    public LessonsPerWeek LessonsPerWeek { get => lessonsPerWeek; set => SetProperty(ref lessonsPerWeek, value); }

    public int DurationMinutes { get => durationMinutes; set => SetProperty(ref durationMinutes, value); }

    public PlanFlexibility Flexibility { get => flexibility; set => SetProperty(ref flexibility, value); }

    public int NumberOfStudents { get => numberOfStudents; set => SetProperty(ref numberOfStudents, value); }

    public DateTime EffectiveDate { get => effectiveDate; set => SetProperty(ref effectiveDate, value); }

    public bool CanEditSelectedPlanFields => SelectedPlan?.State == PlanCatalogState.Draft;

    public bool CanDeleteSelectedPlan => SelectedPlan?.State == PlanCatalogState.Draft;

    public bool CanPublishSelectedPlan => SelectedPlan?.State is PlanCatalogState.Draft or PlanCatalogState.Archived;

    public bool CanArchiveSelectedPlan => SelectedPlan?.State == PlanCatalogState.Active;

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

    public AsyncRelayCommand DeleteDraftPlanCommand { get; }

    public AsyncRelayCommand PublishPlanCommand { get; }

    public AsyncRelayCommand ArchivePlanCommand { get; }

    public Task LoadAsync() => ReloadAsync();

    private async Task AddDraftPlanAsync()
    {
        var result = await planCatalogService.CreateDraftAsync(
            new PlanDraftInput(
                "New plan",
                400m,
                LessonsPerWeek.One,
                DurationMinutes: 60,
                PlanFlexibility.MoveOrCancelByAgreement,
                NumberOfStudents: 1,
                new DateTimeOffset(EffectiveDate.Date, TimeSpan.Zero)),
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

        var result = await planCatalogService.UpdateDraftAsync(
            SelectedPlan.Id,
            CreateDraftInput(),
            CancellationToken.None);
        if (!result.Succeeded)
        {
            SetError(result.Errors.FirstOrDefault()?.Message ?? "Plan could not be saved.");
            return;
        }

        ErrorMessage = null;
        await ReloadAsync(result.Value!.Id);
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

    private async Task PublishPlanAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var result = await planCatalogService.PublishAsync(SelectedPlan.Id, CancellationToken.None);
        if (!result.Succeeded)
        {
            SetError(result.Errors.FirstOrDefault()?.Message ?? "Plan could not be published.");
            return;
        }

        ErrorMessage = null;
        await ReloadAsync(result.Value!.Id);
    }

    private async Task ArchivePlanAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var result = await planCatalogService.ArchiveAsync(SelectedPlan.Id, CancellationToken.None);
        if (!result.Succeeded)
        {
            SetError(result.Errors.FirstOrDefault()?.Message ?? "Plan could not be archived.");
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
        }

        OnPropertyChanged(nameof(CanEditSelectedPlanFields));
        OnPropertyChanged(nameof(CanDeleteSelectedPlan));
        OnPropertyChanged(nameof(CanPublishSelectedPlan));
        OnPropertyChanged(nameof(CanArchiveSelectedPlan));
        SaveDraftPlanCommand.RaiseCanExecuteChanged();
        DeleteDraftPlanCommand.RaiseCanExecuteChanged();
        PublishPlanCommand.RaiseCanExecuteChanged();
        ArchivePlanCommand.RaiseCanExecuteChanged();
    }

    private PlanDraftInput CreateDraftInput() =>
        new(
            Name,
            PriceWithoutVat,
            LessonsPerWeek,
            DurationMinutes,
            Flexibility,
            NumberOfStudents,
            new DateTimeOffset(EffectiveDate.Date, TimeSpan.Zero));

    private void SetError(string message)
    {
        ErrorMessage = message;
    }

    private static int StateSortOrder(PlanCatalogState state) =>
        state switch
        {
            PlanCatalogState.Draft => 0,
            PlanCatalogState.Active => 1,
            PlanCatalogState.Archived => 2,
            _ => 3
        };
}
