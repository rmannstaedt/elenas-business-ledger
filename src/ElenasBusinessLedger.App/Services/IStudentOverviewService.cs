using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.App.Services;

public sealed record StudentOverviewSnapshot(
    StudentRecord Record,
    IReadOnlyList<IssuedInvoiceSummary> Invoices)
{
    public Student Student => Record.Student;
}

public interface IStudentOverviewService
{
    Task<IReadOnlyList<StudentOverviewSnapshot>> LoadAsync(CancellationToken cancellationToken);

    Task<OperationResult<StudentOverviewSnapshot>> SetStudentActiveAsync(
        string studentShortName,
        bool isActive,
        CancellationToken cancellationToken);
}
