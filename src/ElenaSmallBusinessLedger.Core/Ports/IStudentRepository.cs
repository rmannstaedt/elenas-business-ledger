namespace ElenaSmallBusinessLedger.Core.Ports;

public interface IStudentRepository
{
    Task<IReadOnlyList<StudentRecord>> ListActiveStudentsAsync(CancellationToken cancellationToken);

    Task<StudentRecord?> GetByShortNameAsync(string shortName, CancellationToken cancellationToken);
}
