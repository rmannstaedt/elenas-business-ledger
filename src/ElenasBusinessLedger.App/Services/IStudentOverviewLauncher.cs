using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.App.Services;

// Lets the dashboard open the student windows and follow their final or changing selection.
public interface IStudentOverviewLauncher
{
    Task<string?> ShowStudentOverviewAsync(
        string? selectedStudentShortName,
        Func<string?, CancellationToken, Task> selectedStudentChangedAsync,
        CancellationToken cancellationToken);

    async Task<StudentChoice?> ShowStudentOverviewForAsync(
        StudentChoice? selectedStudent, Func<StudentChoice?, CancellationToken, Task> selectionChanged, CancellationToken token)
    {
        var shortName = await ShowStudentOverviewAsync(selectedStudent?.ShortName,
            (name, cancellation) => selectionChanged(name is null ? null : new StudentChoice(0, name), cancellation), token);
        return shortName is null ? null : new StudentChoice(0, shortName);
    }

    async Task<StudentChoice?> ShowStudentDetailsForAsync(StudentChoice? selectedStudent, CancellationToken token)
    {
        var shortName = await ShowStudentDetailsAsync(selectedStudent?.ShortName, token);
        return shortName is null ? null : new StudentChoice(0, shortName);
    }

    Task<string?> ShowStudentDetailsAsync(
        string? selectedStudentShortName,
        CancellationToken cancellationToken);
}
