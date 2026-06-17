using System.Windows;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;

namespace MySmallBusinessLedger.App.Services;

public sealed class StudentEditorWindowLauncher : IStudentEditorLauncher
{
    private readonly IStudentEditorService studentEditorService;
    private readonly IPlanCatalogService planCatalogService;

    public StudentEditorWindowLauncher(
        IStudentEditorService studentEditorService,
        IPlanCatalogService planCatalogService)
    {
        this.studentEditorService = studentEditorService;
        this.planCatalogService = planCatalogService;
    }

    public async Task<bool> ShowAddStudentAsync(CancellationToken cancellationToken)
    {
        var assignablePlans = await planCatalogService.ListAssignableAsync(cancellationToken);
        var viewModel = StudentEditorViewModel.CreateNew(studentEditorService, assignablePlans);
        return ShowDialog(viewModel);
    }

    public async Task<bool> ShowEditStudentAsync(string studentShortName, CancellationToken cancellationToken)
    {
        var assignablePlansTask = planCatalogService.ListAssignableAsync(cancellationToken);
        var snapshotTask = studentEditorService.LoadAsync(studentShortName, cancellationToken);
        await Task.WhenAll(assignablePlansTask, snapshotTask);

        var snapshot = await snapshotTask;
        if (snapshot is null)
        {
            MessageBox.Show(
                $"Student '{studentShortName}' could not be found.",
                "Edit Student",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        var viewModel = StudentEditorViewModel.FromSnapshot(
            studentEditorService,
            snapshot,
            await assignablePlansTask);
        return ShowDialog(viewModel);
    }

    private static bool ShowDialog(StudentEditorViewModel viewModel)
    {
        var window = new StudentEditorView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        return window.ShowDialog() == true && viewModel.WasSaved;
    }
}
