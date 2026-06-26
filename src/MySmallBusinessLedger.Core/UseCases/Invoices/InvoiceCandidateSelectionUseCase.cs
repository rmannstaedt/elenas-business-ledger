using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Invoices;

public sealed class InvoiceCandidateSelectionUseCase
{
    private readonly IStudentRepository studentRepository;
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly ILessonRepository? lessonRepository;
    private readonly TimeSpan lessonDuration;

    public InvoiceCandidateSelectionUseCase(
        IStudentRepository studentRepository,
        IGoogleCalendarGateway calendarGateway,
        IInvoiceRepository invoiceRepository,
        TimeSpan? lessonDuration = null,
        ILessonRepository? lessonRepository = null)
    {
        this.studentRepository = studentRepository;
        this.calendarGateway = calendarGateway;
        this.invoiceRepository = invoiceRepository;
        this.lessonRepository = lessonRepository;
        this.lessonDuration = lessonDuration ?? TimeSpan.FromHours(1);
    }

    public async Task<OperationResult<InvoiceCandidateSelectionResult>> ExecuteAsync(
        DateOnly date,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var students = await studentRepository.ListActiveStudentsAsync(cancellationToken);
        if (lessonRepository is not null)
        {
            return await ExecuteFromLocalLessonsAsync(students, date, now, cancellationToken);
        }

        var lessons = await calendarGateway.ListLessonsAsync(date, date, cancellationToken);
        var month = new DateOnly(date.Year, date.Month, 1);
        var candidates = new List<string>();

        foreach (var shortName in lessons.Select(lesson => lesson.StudentShortName).Distinct(StringComparer.Ordinal))
        {
            if (!await invoiceRepository.HasInvoiceForStudentInMonthAsync(shortName, month, cancellationToken))
            {
                candidates.Add(shortName);
            }
        }

        var candidateSet = candidates.ToHashSet(StringComparer.Ordinal);
        var eligibleLessons = lessons
            .Where(lesson => candidateSet.Contains(lesson.StudentShortName))
            .OrderBy(lesson => lesson.StartsAt)
            .ToArray();

        var preselected = eligibleLessons
            .Where(lesson => lesson.StartsAt <= now && now < lesson.StartsAt.Add(lessonDuration))
            .Select(lesson => lesson.StudentShortName)
            .FirstOrDefault();

        preselected ??= eligibleLessons
            .Where(lesson => lesson.StartsAt >= now && lesson.StartsAt - now <= TimeSpan.FromMinutes(30))
            .Select(lesson => lesson.StudentShortName)
            .FirstOrDefault();

        var allStudents = students
            .Select(student => student.Student.ShortName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return OperationResult<InvoiceCandidateSelectionResult>.Success(new InvoiceCandidateSelectionResult(
            candidates.Order(StringComparer.Ordinal).ToArray(),
            allStudents,
            preselected));
    }

    private async Task<OperationResult<InvoiceCandidateSelectionResult>> ExecuteFromLocalLessonsAsync(
        IReadOnlyList<StudentRecord> students,
        DateOnly date,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var lessons = await lessonRepository!.ListForWindowAsync(date, date, cancellationToken);
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
