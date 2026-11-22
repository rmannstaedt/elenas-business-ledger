using System.Windows;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.App.Views;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.App.Services;

// Coordinates the student list and details windows and returns selection changes to the dashboard.
public sealed class StudentOverviewWindowLauncher : IStudentOverviewLauncher
{
    private readonly IStudentOverviewService studentOverviewService;
    private readonly IStudentEditorService studentEditorService;
    private readonly IPlanCatalogService planCatalogService;
    private readonly IInvoiceWorkflowLauncher invoiceWorkflowLauncher;
    private readonly IUiClock clock;

    public StudentOverviewWindowLauncher(
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
        this.clock = clock;
    }

    public async Task<string?> ShowStudentOverviewAsync(
        string? selectedStudentShortName,
        Func<string?, CancellationToken, Task> selectedStudentChangedAsync,
        CancellationToken cancellationToken) =>
        (await ShowStudentOverviewForAsync(selectedStudentShortName is null ? null : new StudentChoice(0, selectedStudentShortName),
            (choice, token) => selectedStudentChangedAsync(choice?.ShortName, token), cancellationToken))?.ShortName;

    // Keeps the Students list and its owned Details window on the same selection and reports changes to the dashboard.
    public async Task<StudentChoice?> ShowStudentOverviewForAsync(
        StudentChoice? selectedStudent, Func<StudentChoice?, CancellationToken, Task> selectionChanged,
        CancellationToken cancellationToken)
    {
        var viewModel = await CreateViewModelAsync(selectedStudent);
        var window = new StudentOverviewView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };
        StudentDetailsView? detailsWindow = null;
        var isOverviewClosing = false;
        window.Loaded += (_, _) =>
        {
            ShowDetailsWindow();
        };
        window.Closing += (_, e) =>
        {
            if (!e.Cancel)
            {
                isOverviewClosing = true;
                detailsWindow?.CloseWithoutPrompt();
                detailsWindow = null;
            }
        };

        viewModel.SelectedStudentChanged += OnSelectedStudentChanged;
        try
        {
            window.ShowDialog();
            return viewModel.SelectedStudent?.Choice;
        }
        finally
        {
            viewModel.SelectedStudentChanged -= OnSelectedStudentChanged;
        }

        async void OnSelectedStudentChanged(object? sender, StudentChoice? choice)
        {
            try
            {
                ShowDetailsWindow();
                await selectionChanged(choice, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        void ShowDetailsWindow()
        {
            if (isOverviewClosing)
            {
                return;
            }

            if (detailsWindow?.IsVisible == true)
            {
                detailsWindow.Activate();
                return;
            }

            detailsWindow = new StudentDetailsView(viewModel)
            {
                Owner = window
            };
            detailsWindow.Closed += OnDetailsWindowClosed;
            detailsWindow.Show();
            detailsWindow.Activate();
        }

        void OnDetailsWindowClosed(object? sender, EventArgs e)
        {
            if (ReferenceEquals(detailsWindow, sender))
            {
                detailsWindow = null;
            }
        }
    }

    public async Task<string?> ShowStudentDetailsAsync(
        string? selectedStudentShortName,
        CancellationToken cancellationToken) =>
        (await ShowStudentDetailsForAsync(selectedStudentShortName is null ? null : new StudentChoice(0, selectedStudentShortName),
            cancellationToken))?.ShortName;

    // Opens Student Details for a dashboard selection and returns the final selected student.
    public async Task<StudentChoice?> ShowStudentDetailsForAsync(StudentChoice? selectedStudent, CancellationToken cancellationToken)
    {
        var viewModel = await CreateViewModelAsync(selectedStudent);
        var window = new StudentDetailsView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        window.ShowDialog();
        return viewModel.SelectedStudent?.Choice;
    }

    private async Task<StudentOverviewViewModel> CreateViewModelAsync(StudentChoice? choice)
    {
        var viewModel = new StudentOverviewViewModel(
            studentOverviewService,
            studentEditorService,
            planCatalogService,
            invoiceWorkflowLauncher,
            clock,
            new MessageBoxStudentOverviewInteractionService());
        await viewModel.LoadAsync(choice?.ShortName, choice?.StudentId);
        return viewModel;
    }
}
