using System.Windows;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;

namespace MySmallBusinessLedger.App.Services;

public sealed class StudentEditorWindowLauncher : IStudentEditorLauncher
{
    private readonly IStudentEditorService studentEditorService;

    public StudentEditorWindowLauncher(IStudentEditorService studentEditorService)
    {
        this.studentEditorService = studentEditorService;
    }

    public Task<bool> ShowAddStudentAsync(CancellationToken cancellationToken)
    {
        var viewModel = StudentEditorViewModel.CreateNew(studentEditorService);
        return Task.FromResult(ShowDialog(viewModel));
    }

    public async Task<bool> ShowEditStudentAsync(string studentShortName, CancellationToken cancellationToken)
    {
        var snapshot = await studentEditorService.LoadAsync(studentShortName, cancellationToken);
        if (snapshot is null)
        {
            MessageBox.Show(
                $"Student '{studentShortName}' could not be found.",
                "Edit Student",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        var viewModel = StudentEditorViewModel.FromSnapshot(studentEditorService, snapshot);
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
