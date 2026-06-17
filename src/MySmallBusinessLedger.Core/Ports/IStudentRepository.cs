using MySmallBusinessLedger.Core.Common;

namespace MySmallBusinessLedger.Core.Ports;

public interface IStudentRepository
{
    Task<IReadOnlyList<StudentRecord>> ListActiveStudentsAsync(CancellationToken cancellationToken);

    Task<StudentRecord?> GetByShortNameAsync(string shortName, CancellationToken cancellationToken);

    Task<OperationResult<StudentRecord>> SaveAsync(StudentRecord student, CancellationToken cancellationToken);
}
