using System.Collections.ObjectModel;
using ElenasBusinessLedger.App.Commands;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Students;
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
    private StudentChoice? selectedStudentBeforeAdd;
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
        CancelStudentCommand = new AsyncRelayCommand(() =>
        {
            ResetSelectionAfterDetailsClose();
            DetailsCloseRequested?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }, () => !IsBusy);
        OpenInvoiceCommand = new AsyncRelayCommand(
            parameter => OpenInvoiceAsync((InvoiceDisplayModel)parameter!),
            parameter => parameter is InvoiceDisplayModel && !IsBusy);
        DismissErrorCommand = new AsyncRelayCommand(() =>
        {
            ErrorMessage = null;
            return Task.CompletedTask;
        });
    }

    public event EventHandler<StudentChoice?>? SelectedStudentChanged;
    public event EventHandler? DetailsCloseRequested;

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

        if (IsBusy)
        {
            OnPropertyChanged(nameof(SelectedStudent));
            return;
        }

        var target = student?.Choice;
        var preparation = await PrepareCurrentEditorForNavigationAsync(target);
        if (preparation == NavigationPreparation.Cancel)
        {
            OnPropertyChanged(nameof(SelectedStudent));
            return;
        }

        if (preparation == NavigationPreparation.Handled)
        {
            return;
        }

        SetSelectedStudentCore(target is null ? null : SelectStudent(target) ?? student);
    }

    public async Task<bool> PrepareForCloseAsync()
    {
        var preparation = await PrepareCurrentEditorForNavigationAsync(SelectedStudent?.Choice);
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
                SelectedStudentChanged?.Invoke(this, SelectedStudent?.Choice);
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

    public AsyncRelayCommand CancelStudentCommand { get; }

    public AsyncRelayCommand OpenInvoiceCommand { get; }

    public AsyncRelayCommand DismissErrorCommand { get; }

    public async Task LoadAsync(string? selectedStudentShortName = null, int? studentId = null)
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

            SetSelectedStudentCore(SelectStudent(selectedStudentShortName is null ? null : new StudentChoice(studentId ?? 0, selectedStudentShortName)) ?? Students.FirstOrDefault());
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

        selectedStudentBeforeAdd = SelectedStudent?.Choice;
        SetSelectedStudentCore(null);
        Invoices.Clear();
        SelectedStudentEditor = StudentEditorViewModel.CreateNew(
            studentEditorService,
            assignablePlans,
            requestCloseOnSave: false);
    }

    private async Task SaveStudentAsync()
    {
        if (await SaveStudentAndSelectAsync(null))
        {
            DetailsCloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task<bool> SaveStudentAndSelectAsync(StudentChoice? selection)
    {
        if (SelectedStudentEditor is not { } editor)
        {
            return true;
        }

        IsBusy = true;
        try
        {
            await editor.SaveCommand.ExecuteAsync();
            if (!editor.WasSaved)
            {
                return false;
            }

            var savedStudentId = SelectedStudent?.Choice.StudentId;
            selectedStudentBeforeAdd = null;
            await LoadAsync(selection?.ShortName ?? editor.ShortName, selection?.StudentId ?? savedStudentId);
            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetSelectionAfterDetailsClose()
    {
        var selectAfterClose = SelectedStudent?.Choice ?? selectedStudentBeforeAdd;
        selectedStudentBeforeAdd = null;
        SetSelectedStudentCore(selectAfterClose is null
            ? Students.FirstOrDefault()
            : SelectStudent(selectAfterClose) ?? Students.FirstOrDefault());
        ApplySelectedStudent();
    }

    private Task OpenInvoiceAsync(InvoiceDisplayModel invoice) =>
        invoiceWorkflowLauncher.ShowInvoiceViewerAsync(invoice.InvoiceNumber, CancellationToken.None);

    private StudentOverviewStudentDisplayModel? SelectStudent(StudentChoice? choice) =>
        choice is null ? null : Students.FirstOrDefault(student => choice.StudentId > 0
            ? student.Choice.StudentId == choice.StudentId
            : StringComparer.OrdinalIgnoreCase.Equals(student.ShortName, choice.ShortName));

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
        CancelStudentCommand.RaiseCanExecuteChanged();
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
            StudentEditSaveChangesChoice.Save => await SaveStudentAndSelectAsync(SelectedStudent?.Choice),
            StudentEditSaveChangesChoice.Discard => true,
            _ => false
        };
    }

    private async Task<NavigationPreparation> PrepareCurrentEditorForNavigationAsync(StudentChoice? selection)
    {
        if (SelectedStudentEditor?.HasChanges != true)
        {
            return NavigationPreparation.Continue;
        }

        var choice = await interactionService.PromptSaveChangesAsync(CancellationToken.None);
        return choice switch
        {
            StudentEditSaveChangesChoice.Save => await SaveStudentAndSelectAsync(selection)
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
