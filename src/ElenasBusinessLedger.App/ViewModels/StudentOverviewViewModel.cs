using System.Collections.ObjectModel;
using ElenasBusinessLedger.App.Commands;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.UseCases.Students;

namespace ElenasBusinessLedger.App.ViewModels;

public sealed class StudentOverviewViewModel : ViewModelBase
{
    private readonly IStudentOverviewService studentOverviewService;
    private readonly IStudentEditorService studentEditorService;
    private readonly IPlanCatalogService planCatalogService;
    private readonly IInvoiceWorkflowLauncher invoiceWorkflowLauncher;
    private readonly IStudentOverviewInteractionService interactionService;
    private StudentOverviewStudentDisplayModel? selectedStudent;
    private StudentEditorViewModel? selectedStudentEditor;
    private IReadOnlyList<PlanCatalogItem> assignablePlans = [];
    private string? selectedStudentBeforeAdd;
    private bool isSelectingStudent;
    private string? errorMessage;
    private bool isBusy;

    public StudentOverviewViewModel(
        IStudentOverviewService studentOverviewService,
        IStudentEditorService studentEditorService,
        IPlanCatalogService planCatalogService,
        IInvoiceWorkflowLauncher invoiceWorkflowLauncher,
        IUiClock clock)
        : this(
            studentOverviewService,
            studentEditorService,
            planCatalogService,
            invoiceWorkflowLauncher,
            clock,
            new DiscardStudentOverviewInteractionService())
    {
    }

    public StudentOverviewViewModel(
        IStudentOverviewService studentOverviewService,
        IStudentEditorService studentEditorService,
        IPlanCatalogService planCatalogService,
        IInvoiceWorkflowLauncher invoiceWorkflowLauncher,
        IUiClock clock,
        IStudentOverviewInteractionService interactionService)
    {
        this.studentOverviewService = studentOverviewService;
        this.studentEditorService = studentEditorService;
        this.planCatalogService = planCatalogService;
        this.invoiceWorkflowLauncher = invoiceWorkflowLauncher;
        this.interactionService = interactionService;

        AddStudentCommand = new AsyncRelayCommand(AddStudentAsync, () => !IsBusy);
        SaveStudentCommand = new AsyncRelayCommand(SaveStudentAsync, () => !IsBusy && SelectedStudentEditor is not null);
        OpenInvoiceCommand = new AsyncRelayCommand(
            parameter => OpenInvoiceAsync((InvoiceDisplayModel)parameter!),
            parameter => parameter is InvoiceDisplayModel && !IsBusy);
        DismissErrorCommand = new AsyncRelayCommand(() =>
        {
            ErrorMessage = null;
            return Task.CompletedTask;
        });
    }

    public event EventHandler<string?>? SelectedStudentChanged;

    public ObservableCollection<StudentOverviewStudentDisplayModel> Students { get; } = [];

    public ObservableCollection<InvoiceDisplayModel> Invoices { get; } = [];

    public StudentOverviewStudentDisplayModel? SelectedStudent
    {
        get => selectedStudent;
        set
        {
            if (isSelectingStudent)
            {
                SetSelectedStudentCore(value);
                return;
            }

            _ = SelectStudentAsync(value);
            OnPropertyChanged(nameof(SelectedStudent));
        }
    }

    public async Task SelectStudentAsync(StudentOverviewStudentDisplayModel? student)
    {
        if (ReferenceEquals(selectedStudent, student))
        {
            return;
        }

        var targetShortName = student?.ShortName;
        var preparation = await PrepareCurrentEditorForNavigationAsync(targetShortName);
        if (preparation == NavigationPreparation.Cancel)
        {
            OnPropertyChanged(nameof(SelectedStudent));
            return;
        }

        if (preparation == NavigationPreparation.Handled)
        {
            return;
        }

        SetSelectedStudentCore(targetShortName is null
            ? null
            : SelectStudent(targetShortName) ?? student);
    }

    public async Task<bool> PrepareForCloseAsync()
    {
        var preparation = await PrepareCurrentEditorForNavigationAsync(SelectedStudent?.ShortName);
        if (preparation == NavigationPreparation.Cancel)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> PrepareForDetailsWindowCloseAsync()
    {
        if (!await PrepareCurrentEditorForDetailsCloseAsync())
        {
            return false;
        }

        ResetSelectionAfterDetailsClose();
        return true;
    }

    private void SetSelectedStudentCore(StudentOverviewStudentDisplayModel? student)
    {
        isSelectingStudent = true;
        try
        {
            if (SetProperty(ref selectedStudent, student, nameof(SelectedStudent)))
            {
                ApplySelectedStudent();
                SelectedStudentChanged?.Invoke(this, SelectedStudent?.ShortName);
            }
        }
        finally
        {
            isSelectingStudent = false;
        }
    }

    public StudentEditorViewModel? SelectedStudentEditor
    {
        get => selectedStudentEditor;
        private set
        {
            if (SetProperty(ref selectedStudentEditor, value))
            {
                OnPropertyChanged(nameof(HasSelectedStudentEditor));
                RaiseCommandAvailabilityChanged();
            }
        }
    }

    public bool HasSelectedStudentEditor => SelectedStudentEditor is not null;

    public bool HasUnsavedStudentChanges => SelectedStudentEditor?.HasChanges == true;

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                RaiseCommandAvailabilityChanged();
            }
        }
    }

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

    public AsyncRelayCommand AddStudentCommand { get; }

    public AsyncRelayCommand SaveStudentCommand { get; }

    public AsyncRelayCommand OpenInvoiceCommand { get; }

    public AsyncRelayCommand DismissErrorCommand { get; }

    public async Task LoadAsync(string? selectedStudentShortName = null)
    {
        IsBusy = true;
        try
        {
            var snapshotsTask = studentOverviewService.LoadAsync(CancellationToken.None);
            var assignablePlansTask = planCatalogService.ListAssignableAsync(CancellationToken.None);
            await Task.WhenAll(snapshotsTask, assignablePlansTask);

            var snapshots = await snapshotsTask;
            assignablePlans = await assignablePlansTask;
            Students.Clear();
            foreach (var snapshot in snapshots.OrderBy(snapshot => snapshot.Student.ShortName, StringComparer.CurrentCulture))
            {
                Students.Add(new StudentOverviewStudentDisplayModel(snapshot));
            }

            SetSelectedStudentCore(SelectStudent(selectedStudentShortName) ?? Students.FirstOrDefault());
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddStudentAsync()
    {
        var preparation = await PrepareCurrentEditorForNavigationAsync(null);
        if (preparation == NavigationPreparation.Cancel)
        {
            return;
        }

        selectedStudentBeforeAdd = SelectedStudent?.ShortName;
        SetSelectedStudentCore(null);
        Invoices.Clear();
        SelectedStudentEditor = StudentEditorViewModel.CreateNew(
            studentEditorService,
            assignablePlans,
            requestCloseOnSave: false);
    }

    private async Task SaveStudentAsync()
    {
        await SaveStudentAndSelectAsync(null);
    }

    private async Task<bool> SaveStudentAndSelectAsync(string? selectedStudentShortName)
    {
        if (SelectedStudentEditor is null)
        {
            return true;
        }

        await SelectedStudentEditor.SaveCommand.ExecuteAsync();
        if (!SelectedStudentEditor.WasSaved)
        {
            return false;
        }

        var savedShortName = SelectedStudentEditor.ShortName;
        selectedStudentBeforeAdd = null;
        await LoadAsync(selectedStudentShortName ?? savedShortName);
        return true;
    }

    private void ResetSelectionAfterDetailsClose()
    {
        var selectAfterClose = SelectedStudent?.ShortName ?? selectedStudentBeforeAdd;
        selectedStudentBeforeAdd = null;
        SetSelectedStudentCore(selectAfterClose is null
            ? Students.FirstOrDefault()
            : SelectStudent(selectAfterClose) ?? Students.FirstOrDefault());
        ApplySelectedStudent();
    }

    private Task OpenInvoiceAsync(InvoiceDisplayModel invoice) =>
        invoiceWorkflowLauncher.ShowInvoiceViewerAsync(invoice.InvoiceNumber, CancellationToken.None);

    private StudentOverviewStudentDisplayModel? SelectStudent(string? shortName)
    {
        if (shortName is null)
        {
            return null;
        }

        return Students.FirstOrDefault(student =>
            StringComparer.OrdinalIgnoreCase.Equals(student.ShortName, shortName));
    }

    private void ApplySelectedStudent()
    {
        var snapshot = SelectedStudent?.Snapshot;
        Invoices.Clear();
        if (snapshot is null)
        {
            SelectedStudentEditor = null;
            RaiseCommandAvailabilityChanged();
            return;
        }

        foreach (var invoice in snapshot.Invoices.OrderByDescending(invoice => invoice.InvoiceDate))
        {
            Invoices.Add(new InvoiceDisplayModel(invoice));
        }

        SelectedStudentEditor = StudentEditorViewModel.FromSnapshot(
            studentEditorService,
            ToEditorSnapshot(snapshot),
            assignablePlans,
            requestCloseOnSave: false);
        RaiseCommandAvailabilityChanged();
    }

    private static StudentEditorSnapshot ToEditorSnapshot(StudentOverviewSnapshot snapshot) =>
        new(
            snapshot.Record.Id,
            snapshot.Student.ShortName,
            snapshot.Student.FullName,
            snapshot.Student.Address,
            snapshot.Student.Phone,
            snapshot.Student.Email,
            snapshot.Student.AgeBracket,
            snapshot.Student.Birthdate,
            snapshot.Student.IsActive,
            snapshot.Record.PlanPeriods.Select(plan => new StudentPlanPeriodInput(
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
            snapshot.Record.Schedule.Select(schedule => new StudentScheduleInput(
                schedule.Weekday,
                schedule.StartsAt)).ToArray());

    private void RaiseCommandAvailabilityChanged()
    {
        AddStudentCommand.RaiseCanExecuteChanged();
        SaveStudentCommand.RaiseCanExecuteChanged();
        OpenInvoiceCommand.RaiseCanExecuteChanged();
    }

    private async Task<bool> PrepareCurrentEditorForDetailsCloseAsync()
    {
        if (SelectedStudentEditor?.HasChanges != true)
        {
            return true;
        }

        var choice = await interactionService.PromptSaveChangesAsync(CancellationToken.None);
        return choice switch
        {
            StudentEditSaveChangesChoice.Save => await SaveStudentAndSelectAsync(SelectedStudent?.ShortName),
            StudentEditSaveChangesChoice.Discard => true,
            _ => false
        };
    }

    private async Task<NavigationPreparation> PrepareCurrentEditorForNavigationAsync(string? selectedStudentShortName)
    {
        if (SelectedStudentEditor?.HasChanges != true)
        {
            return NavigationPreparation.Continue;
        }

        var choice = await interactionService.PromptSaveChangesAsync(CancellationToken.None);
        return choice switch
        {
            StudentEditSaveChangesChoice.Save => await SaveStudentAndSelectAsync(selectedStudentShortName)
                ? NavigationPreparation.Handled
                : NavigationPreparation.Cancel,
            StudentEditSaveChangesChoice.Discard => NavigationPreparation.Continue,
            _ => NavigationPreparation.Cancel
        };
    }

    private enum NavigationPreparation
    {
        Continue,
        Handled,
        Cancel
    }
}
