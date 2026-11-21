using System.Windows;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.App.Views;

namespace ElenasBusinessLedger.App.Services;

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
        CancellationToken cancellationToken)
    {
        var viewModel = await CreateViewModelAsync(selectedStudentShortName);
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
            return viewModel.SelectedStudent?.ShortName;
        }
        finally
        {
            viewModel.SelectedStudentChanged -= OnSelectedStudentChanged;
        }

        async void OnSelectedStudentChanged(object? sender, string? selectedShortName)
        {
            try
            {
                ShowDetailsWindow();
                await selectedStudentChangedAsync(selectedShortName, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        var viewModel = await CreateViewModelAsync(selectedStudentShortName);
        var window = new StudentDetailsView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        window.ShowDialog();
        return viewModel.SelectedStudent?.ShortName;
    }

    private async Task<StudentOverviewViewModel> CreateViewModelAsync(string? selectedStudentShortName)
    {
        var viewModel = new StudentOverviewViewModel(
            studentOverviewService,
            studentEditorService,
            planCatalogService,
            invoiceWorkflowLauncher,
            clock,
            new MessageBoxStudentOverviewInteractionService());
        await viewModel.LoadAsync(selectedStudentShortName);
        return viewModel;
    }
}
