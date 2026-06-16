namespace ElenaSmallBusinessLedger.App.Services;

public interface IStudentEditorLauncher
{
    Task<bool> ShowAddStudentAsync(CancellationToken cancellationToken);

    Task<bool> ShowEditStudentAsync(string studentShortName, CancellationToken cancellationToken);
}
