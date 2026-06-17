using System.Collections.ObjectModel;
using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;

namespace MySmallBusinessLedger.App.ViewModels;

public sealed class InvoiceCandidateDialogViewModel : ViewModelBase
{
    private readonly IInvoiceWorkflowService invoiceWorkflowService;
    private readonly DateOnly date;
    private readonly DateTimeOffset now;
    private string? selectedStudentShortName;
    private string? errorMessage;

    public InvoiceCandidateDialogViewModel(
        IInvoiceWorkflowService invoiceWorkflowService,
        DateOnly date,
        DateTimeOffset now)
    {
        this.invoiceWorkflowService = invoiceWorkflowService;
        this.date = date;
        this.now = now;

        ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, () => SelectedStudentShortName is not null);
    }

    public ObservableCollection<string> CandidateStudentShortNames { get; } = [];

    public ObservableCollection<string> AllStudentShortNames { get; } = [];

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

        SelectedStudentShortName = result.Value.PreselectedStudentShortName
            ?? CandidateStudentShortNames.FirstOrDefault()
            ?? AllStudentShortNames.FirstOrDefault();
        ErrorMessage = null;
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
