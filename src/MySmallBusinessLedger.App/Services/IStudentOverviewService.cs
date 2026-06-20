using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.Students;

namespace MySmallBusinessLedger.App.Services;

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
