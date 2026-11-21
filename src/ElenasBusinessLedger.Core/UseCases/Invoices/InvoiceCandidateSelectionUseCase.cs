using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

public sealed class InvoiceCandidateSelectionUseCase
{
    private readonly IStudentRepository studentRepository;
    private readonly ILessonRepository lessonRepository;
    private readonly TimeSpan lessonDuration;

    public InvoiceCandidateSelectionUseCase(
        IStudentRepository studentRepository,
        ILessonRepository lessonRepository,
        TimeSpan? lessonDuration = null)
    {
        this.studentRepository = studentRepository;
        this.lessonRepository = lessonRepository;
        this.lessonDuration = lessonDuration ?? TimeSpan.FromHours(1);
    }

    public async Task<OperationResult<InvoiceCandidateSelectionResult>> ExecuteAsync(
        DateOnly date,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var students = await studentRepository.ListActiveStudentsAsync(cancellationToken);
        var lessons = await lessonRepository.ListForWindowAsync(date, date, cancellationToken);
        var eligibleLessons = lessons
            .Where(lesson => lesson.InvoiceNumber is null)
            .OrderBy(lesson => lesson.StartsAt)
            .ToArray();
        var candidates = eligibleLessons
            .Select(lesson => lesson.StudentShortName)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var candidateSet = candidates.ToHashSet(StringComparer.Ordinal);
        var preselected = eligibleLessons
            .Where(lesson => candidateSet.Contains(lesson.StudentShortName))
            .Where(lesson => lesson.StartsAt <= now && now < lesson.StartsAt.Add(lessonDuration))
            .Select(lesson => lesson.StudentShortName)
            .FirstOrDefault();

        preselected ??= eligibleLessons
            .Where(lesson => candidateSet.Contains(lesson.StudentShortName))
            .Where(lesson => lesson.StartsAt >= now && lesson.StartsAt - now <= TimeSpan.FromMinutes(30))
            .Select(lesson => lesson.StudentShortName)
            .FirstOrDefault();

        var allStudents = students
            .Select(student => student.Student.ShortName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return OperationResult<InvoiceCandidateSelectionResult>.Success(new InvoiceCandidateSelectionResult(
            candidates,
            allStudents,
            preselected));
    }
}
