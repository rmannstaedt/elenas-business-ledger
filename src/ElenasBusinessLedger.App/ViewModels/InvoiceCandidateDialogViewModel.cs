using System.Collections.ObjectModel;
using ElenasBusinessLedger.App.Commands;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.App.ViewModels;

// Loads and selects student choices before the invoice preview builds a draft.
public sealed class InvoiceCandidateDialogViewModel : ViewModelBase
{
    private readonly IInvoiceWorkflowService invoiceWorkflowService;
    private readonly DateOnly date;
    private readonly DateTimeOffset now;
    private readonly string? initialStudentShortName;
    private readonly int? initialStudentId;
    private StudentChoice? selectedStudent;
    private string? selectedStudentShortName;
    private string? errorMessage;

    public InvoiceCandidateDialogViewModel(
        IInvoiceWorkflowService invoiceWorkflowService,
        DateOnly date,
        DateTimeOffset now,
        string? initialStudentShortName = null,
        int? initialStudentId = null)
    {
        this.invoiceWorkflowService = invoiceWorkflowService;
        this.date = date;
        this.now = now;
        this.initialStudentShortName = initialStudentShortName;
        this.initialStudentId = initialStudentId;

        ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, () => SelectedStudentShortName is not null);
    }

    public ObservableCollection<string> CandidateStudentShortNames { get; } = [];

    public ObservableCollection<string> AllStudentShortNames { get; } = [];

    public ObservableCollection<StudentChoice> CandidateChoices { get; } = [];

    public ObservableCollection<StudentChoice> StudentChoices { get; } = [];

    public StudentChoice? SelectedStudent
    {
        get => selectedStudent;
        set
        {
            if (value is not null && SetProperty(ref selectedStudent, value))
            {
                SelectedStudentShortName = value.ShortName;
            }
        }
    }

    public string? SelectedStudentShortName
    {
        get => selectedStudentShortName;
        set
        {
            if (SetProperty(ref selectedStudentShortName, value))
            {
                ConfirmCommand.RaiseCanExecuteChanged();
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

    public bool WasConfirmed { get; private set; }

    public AsyncRelayCommand ConfirmCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    // Loads choices for the invoice picker and prefers the student already selected on the dashboard.
    public async Task LoadAsync()
    {
        var result = await invoiceWorkflowService.LoadCandidatesAsync(date, now, CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not load invoice candidates.";
            return;
        }

        CandidateStudentShortNames.Clear();
        foreach (var shortName in result.Value!.CandidateStudentShortNames)
        {
            CandidateStudentShortNames.Add(shortName);
        }

        AllStudentShortNames.Clear();
        foreach (var shortName in result.Value.AllStudentShortNames)
        {
            AllStudentShortNames.Add(shortName);
        }

        CandidateChoices.Clear();
        StudentChoices.Clear();
        foreach (var choice in (result.Value.StudentChoices ?? []).Where(choice => choice.StudentId > 0))
        {
            StudentChoices.Add(choice);
        }
        foreach (var choice in result.Value.CandidateChoices ?? [])
        {
            CandidateChoices.Add(choice);
        }

        SelectedStudent = StudentChoices.FirstOrDefault(choice => choice.StudentId == initialStudentId)
            ?? StudentChoices.Concat(CandidateChoices).FirstOrDefault(choice => choice.StudentId == result.Value.PreselectedStudentId)
            ?? CandidateChoices.FirstOrDefault() ?? StudentChoices.FirstOrDefault();
        if (SelectedStudent is null)
        {
            SelectedStudentShortName = PreferredInitialStudent(result.Value.AllStudentShortNames)
            ?? result.Value.PreselectedStudentShortName
            ?? CandidateStudentShortNames.FirstOrDefault()
                ?? AllStudentShortNames.FirstOrDefault();
            foreach (var shortName in CandidateStudentShortNames)
            {
                CandidateChoices.Add(new StudentChoice(0, shortName));
            }
            foreach (var shortName in AllStudentShortNames)
            {
                StudentChoices.Add(new StudentChoice(0, shortName));
            }
            SelectedStudent = StudentChoices.Concat(CandidateChoices)
                .FirstOrDefault(choice => choice.ShortName == SelectedStudentShortName);
        }

        ErrorMessage = null;
    }

    private string? PreferredInitialStudent(IReadOnlyList<string> allStudentShortNames)
    {
        if (string.IsNullOrWhiteSpace(initialStudentShortName))
        {
            return null;
        }

        return allStudentShortNames.FirstOrDefault(
            shortName => string.Equals(shortName, initialStudentShortName, StringComparison.OrdinalIgnoreCase));
    }

    private Task ConfirmAsync()
    {
        if (SelectedStudentShortName is null)
        {
            ErrorMessage = "Select a student before continuing.";
            return Task.CompletedTask;
        }

        WasConfirmed = true;
        CloseRequested?.Invoke(this, true);
        return Task.CompletedTask;
    }
}
