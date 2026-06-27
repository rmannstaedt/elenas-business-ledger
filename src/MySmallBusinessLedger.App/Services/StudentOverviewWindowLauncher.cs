using System.Windows;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;

namespace MySmallBusinessLedger.App.Services;

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
        var viewModel = new StudentOverviewViewModel(
            studentOverviewService,
            studentEditorService,
            planCatalogService,
            invoiceWorkflowLauncher,
            clock,
            new MessageBoxStudentOverviewInteractionService());
        await viewModel.LoadAsync(selectedStudentShortName);
        var window = new StudentOverviewView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };
        var detailsWindow = new StudentDetailsView(viewModel);
        window.Loaded += (_, _) =>
        {
            if (!detailsWindow.IsVisible)
            {
                detailsWindow.Owner = window;
                detailsWindow.Show();
            }
        };
        window.Closing += (_, e) =>
        {
            if (!e.Cancel)
            {
                detailsWindow.CloseWithoutPrompt();
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
                await selectedStudentChangedAsync(selectedShortName, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }
    }
}
