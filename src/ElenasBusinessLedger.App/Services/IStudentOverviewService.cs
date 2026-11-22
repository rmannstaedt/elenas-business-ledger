using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.App.Services;

// Pairs a student with issued invoice summaries for the shared list and details editor.
public sealed record StudentOverviewSnapshot(
    StudentRecord Record,
    IReadOnlyList<IssuedInvoiceSummary> Invoices)
{
    public Student Student => Record.Student;
}

// Supplies Manage Students data and handles changes to student availability.
public interface IStudentOverviewService
{
    Task<IReadOnlyList<StudentOverviewSnapshot>> LoadAsync(CancellationToken cancellationToken);

    Task<OperationResult<StudentOverviewSnapshot>> SetStudentActiveAsync(
        string studentShortName,
        bool isActive,
        CancellationToken cancellationToken);
}
