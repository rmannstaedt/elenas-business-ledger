using System.Collections.ObjectModel;
using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.UseCases.Students;

namespace MySmallBusinessLedger.App.ViewModels;

public sealed class StudentOverviewViewModel : ViewModelBase
{
    private readonly IStudentOverviewService studentOverviewService;
    private readonly IStudentEditorService studentEditorService;
    private readonly IPlanCatalogService planCatalogService;
    private readonly IInvoiceWorkflowLauncher invoiceWorkflowLauncher;
    private StudentOverviewStudentDisplayModel? selectedStudent;
    private StudentEditorViewModel? selectedStudentEditor;
    private IReadOnlyList<PlanCatalogItem> assignablePlans = [];
    private string? selectedStudentBeforeAdd;
    private string? errorMessage;
    private bool isBusy;

    public StudentOverviewViewModel(
        IStudentOverviewService studentOverviewService,
        IStudentEditorService studentEditorService,
        IPlanCatalogService planCatalogService,
        IInvoiceWorkflowLauncher invoiceWorkflowLauncher,
        IUiClock clock)
    {
        this.studentOverviewService = studentOverviewService;
        this.studentEditorService = studentEditorService;
        this.planCatalogService = planCatalogService;
        this.invoiceWorkflowLauncher = invoiceWorkflowLauncher;

        AddStudentCommand = new AsyncRelayCommand(AddStudentAsync, () => !IsBusy);
        SaveStudentCommand = new AsyncRelayCommand(SaveStudentAsync, () => !IsBusy && SelectedStudentEditor is not null);
        CancelStudentEditCommand = new AsyncRelayCommand(CancelStudentEditAsync, () => !IsBusy && SelectedStudentEditor is not null);
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
            if (SetProperty(ref selectedStudent, value))
            {
                ApplySelectedStudent();
                SelectedStudentChanged?.Invoke(this, SelectedStudent?.ShortName);
            }
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

    public AsyncRelayCommand CancelStudentEditCommand { get; }

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

            SelectedStudent = SelectStudent(selectedStudentShortName) ?? Students.FirstOrDefault();
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
        selectedStudentBeforeAdd = SelectedStudent?.ShortName;
        SelectedStudent = null;
        Invoices.Clear();
        SelectedStudentEditor = StudentEditorViewModel.CreateNew(
            studentEditorService,
            assignablePlans,
            requestCloseOnSave: false);
    }

    private async Task SaveStudentAsync()
    {
        if (SelectedStudentEditor is null)
        {
            return;
        }

        await SelectedStudentEditor.SaveCommand.ExecuteAsync();
        if (!SelectedStudentEditor.WasSaved)
        {
            return;
        }

        var savedShortName = SelectedStudentEditor.ShortName;
        selectedStudentBeforeAdd = null;
        await LoadAsync(savedShortName);
    }

    private Task CancelStudentEditAsync()
    {
        var selectAfterCancel = SelectedStudent?.ShortName ?? selectedStudentBeforeAdd;
        selectedStudentBeforeAdd = null;
        SelectedStudent = selectAfterCancel is null
            ? Students.FirstOrDefault()
            : SelectStudent(selectAfterCancel) ?? Students.FirstOrDefault();
        ApplySelectedStudent();
        return Task.CompletedTask;
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
                plan.Plan.PaymentTerms)).ToArray(),
            snapshot.Record.Schedule.Select(schedule => new StudentScheduleInput(
                schedule.Weekday,
                schedule.StartsAt)).ToArray());

    private void RaiseCommandAvailabilityChanged()
    {
        AddStudentCommand.RaiseCanExecuteChanged();
        SaveStudentCommand.RaiseCanExecuteChanged();
        CancelStudentEditCommand.RaiseCanExecuteChanged();
        OpenInvoiceCommand.RaiseCanExecuteChanged();
    }
}
