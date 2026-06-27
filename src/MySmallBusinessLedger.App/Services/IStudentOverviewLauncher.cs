namespace MySmallBusinessLedger.App.Services;

public interface IStudentOverviewLauncher
{
    Task<string?> ShowStudentOverviewAsync(
        string? selectedStudentShortName,
        Func<string?, CancellationToken, Task> selectedStudentChangedAsync,
        CancellationToken cancellationToken);

    Task<string?> ShowStudentDetailsAsync(
        string? selectedStudentShortName,
        CancellationToken cancellationToken);
}
