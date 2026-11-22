using ElenasBusinessLedger.Core.Common;

namespace ElenasBusinessLedger.Core.Ports;

// Persists student identity, plans, and schedule while supporting lookup by ID or stored short name.
public interface IStudentRepository
{
    Task<IReadOnlyList<StudentRecord>> ListStudentsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<StudentRecord>> ListActiveStudentsAsync(CancellationToken cancellationToken);

    async Task<StudentRecord?> GetByIdAsync(int studentId, CancellationToken cancellationToken) =>
        (await ListStudentsAsync(cancellationToken)).FirstOrDefault(student => student.Id == studentId);

    Task<StudentRecord?> GetByShortNameAsync(string shortName, CancellationToken cancellationToken);

    Task<OperationResult<StudentRecord>> SaveAsync(StudentRecord student, CancellationToken cancellationToken);
}
