using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.App.Services;

// Supplies student records with invoice history for Manage Students and Student Details.
public sealed class CoreStudentOverviewService : IStudentOverviewService
{
    private readonly IStudentRepository studentRepository;
    private readonly IInvoiceRepository invoiceRepository;

    public CoreStudentOverviewService(
        IStudentRepository studentRepository,
        IInvoiceRepository invoiceRepository)
    {
        this.studentRepository = studentRepository;
        this.invoiceRepository = invoiceRepository;
    }

    // Pairs each stored student with their issued invoices for the shared Manage Students and Details views.
    public async Task<IReadOnlyList<StudentOverviewSnapshot>> LoadAsync(CancellationToken cancellationToken)
    {
        var students = await studentRepository.ListStudentsAsync(cancellationToken);
        var snapshots = new List<StudentOverviewSnapshot>(students.Count);
        foreach (var student in students)
        {
            snapshots.Add(new StudentOverviewSnapshot(
                student,
                await invoiceRepository.ListInvoicesForStudentAsync(student.Id, cancellationToken)));
        }

        return snapshots;
    }

    // Changes the student's availability and returns their refreshed details and invoice history.
    public async Task<OperationResult<StudentOverviewSnapshot>> SetStudentActiveAsync(
        string studentShortName,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var student = await studentRepository.GetByShortNameAsync(studentShortName, cancellationToken);
        if (student is null)
        {
            return OperationResult<StudentOverviewSnapshot>.Failure(
                new ValidationError("Student.NotFound", "Student was not found."));
        }

        var updated = student with
        {
            Student = student.Student with { IsActive = isActive }
        };
        var saveResult = await studentRepository.SaveAsync(updated, cancellationToken);
        if (!saveResult.Succeeded)
        {
            return OperationResult<StudentOverviewSnapshot>.Failure(saveResult.Errors.ToArray());
        }

        return OperationResult<StudentOverviewSnapshot>.Success(
            new StudentOverviewSnapshot(
                saveResult.Value!,
                await invoiceRepository.ListInvoicesForStudentAsync(saveResult.Value!.Id, cancellationToken)));
    }

}
