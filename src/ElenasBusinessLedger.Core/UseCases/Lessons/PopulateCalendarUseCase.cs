using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

// Creates missing monthly schedule entries and saves acknowledged writes as local billing lessons.
public sealed class PopulateCalendarUseCase
{
    private readonly IStudentRepository studentRepository;
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly TimeZoneInfo lessonTimeZone;
    private readonly ILessonRepository lessonRepository;

    public PopulateCalendarUseCase(
        IStudentRepository studentRepository,
        IGoogleCalendarGateway calendarGateway,
        TimeZoneInfo lessonTimeZone,
        ILessonRepository lessonRepository)
    {
        this.studentRepository = studentRepository;
        this.calendarGateway = calendarGateway;
        this.lessonTimeZone = lessonTimeZone;
        this.lessonRepository = lessonRepository;
    }

    // Creates missing scheduled lessons for the month and stores each successful calendar write in the billing ledger.
    public async Task<OperationResult<PopulateCalendarResult>> ExecuteAsync(
        PopulateCalendarRequest request,
        CancellationToken cancellationToken)
    {
        var firstOfDisplayMonth = new DateOnly(request.DisplayMonth.Year, request.DisplayMonth.Month, 1);
        var student = request.StudentId is { } id
            ? await studentRepository.GetByIdAsync(id, cancellationToken)
            : await studentRepository.GetByShortNameAsync(request.StudentShortName, cancellationToken);
        if (student is null)
        {
            return OperationResult<PopulateCalendarResult>.Failure(new ValidationError(
                "CalendarPopulate.StudentNotFound",
                "The selected student could not be found."));
        }

        if (student.Schedule.Count == 0)
        {
            return OperationResult<PopulateCalendarResult>.Failure(new ValidationError(
                "CalendarPopulate.NoSchedule",
                "The selected student has no saved schedule to populate."));
        }

        var studentShortName = student.Student.ShortName;
        var lastOfDisplayMonth = firstOfDisplayMonth.AddMonths(1).AddDays(-1);
        var existingLessons = await calendarGateway.ListLessonsAsync(firstOfDisplayMonth, lastOfDisplayMonth, cancellationToken);
        var localLessons = await lessonRepository.ListForStudentMonthAsync(studentShortName, firstOfDisplayMonth, cancellationToken, request.StudentId);
        var linkedLessons = await lessonRepository.ListByGoogleCalendarEventIdsAsync(
            existingLessons.Select(lesson => lesson.Id).Distinct(StringComparer.Ordinal).ToArray(),
            cancellationToken);
        var ownersByEventId = linkedLessons.ToDictionary(lesson => lesson.GoogleCalendarEventId!, lesson => lesson.StudentId);
        var existingStarts = existingLessons
            .Where(lesson => lesson.HasKnownStartTime &&
                (ownersByEventId.TryGetValue(lesson.Id, out var ownerId)
                    ? ownerId == student.Id
                    : string.Equals(lesson.StudentShortName, studentShortName, StringComparison.OrdinalIgnoreCase)))
            .Select(lesson => lesson.StartsAt)
            .Concat(localLessons
                .Where(lesson => lesson.StudentId == student.Id && lesson.InvoiceNumber is not null && lesson.HasKnownStartTime)
                .Select(lesson => lesson.StartsAt))
            .ToHashSet();

        var candidates = CreateScheduledStarts(student.Schedule, firstOfDisplayMonth, lastOfDisplayMonth)
            .Distinct()
            .Select(startsAt => new
            {
                StartsAt = startsAt,
                Plan = StudentPlanPeriod.GetActivePlan(
                    student.PlanPeriods,
                    DateOnly.FromDateTime(startsAt.DateTime))
            })
            .Where(candidate => candidate.Plan is not null)
            .Select(candidate => new ScheduledLessonCandidate(
                new LessonAddRequest(
                    studentShortName,
                    candidate.StartsAt,
                    candidate.Plan!.PlanCatalogId),
                candidate.Plan!))
            .ToArray();
        var requestsToCreate = candidates
            .Where(candidate => !existingStarts.Contains(candidate.Request.StartsAt))
            .ToArray();
        var skippedDuplicates = candidates.Length - requestsToCreate.Length;

        if (requestsToCreate.Length == 0)
        {
            return OperationResult<PopulateCalendarResult>.Success(new PopulateCalendarResult(
                CreatedCount: 0,
                skippedDuplicates,
                existingLessons));
        }

        var savedCount = 0;
        var populate = await calendarGateway.PopulateWithReceiptsAsync(
            requestsToCreate.Select(candidate => candidate.Request).ToArray(),
            firstOfDisplayMonth,
            SaveReceiptAsync,
            cancellationToken);
        var result = new PopulateCalendarResult(savedCount, skippedDuplicates, populate.Value ?? []);
        return populate.Succeeded
            ? OperationResult<PopulateCalendarResult>.Success(result)
            : OperationResult<PopulateCalendarResult>.Failure(result, populate.Errors.ToArray());

        async Task<ValidationError?> SaveReceiptAsync(int index, CalendarLesson created, CancellationToken token)
        {
            var candidate = requestsToCreate[index];
            try
            {
                await lessonRepository.SaveAsync(
                    new LessonRecord(
                        Id: 0,
                        StudentId: student.Id,
                        StudentShortName: studentShortName,
                        PlanCatalogId: candidate.Plan.PlanCatalogId,
                        PlanName: candidate.Plan.Name,
                        StartsAt: candidate.Request.StartsAt,
                        DurationMinutes: candidate.Plan.DurationMinutes,
                        State: created.State,
                        GoogleCalendarEventId: created.Id,
                        InvoiceNumber: null,
                        HasKnownStartTime: candidate.Request.HasKnownStartTime),
                    token);
                savedCount++;
                return null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var saved = await lessonRepository.ListByGoogleCalendarEventIdsAsync([created.Id], token);
                if (saved.Any(lesson => lesson.StudentId == student.Id && lesson.PlanCatalogId == candidate.Plan.PlanCatalogId))
                {
                    savedCount++;
                    return null;
                }

                var cleanup = await calendarGateway.DeleteWithoutRefreshAsync(created.Id, firstOfDisplayMonth, token);
                return new ValidationError("Lesson.LocalSaveFailed", cleanup.State == CalendarWriteState.Acknowledged
                    ? "The new appointment could not be saved to the billing ledger and was removed from Google Calendar."
                    : "The new appointment could not be saved to the billing ledger. Check the exact appointment in Google Calendar before retrying.");
            }
        }
    }

    private IEnumerable<DateTimeOffset> CreateScheduledStarts(
        IReadOnlyList<StudentScheduleEntry> schedule,
        DateOnly from,
        DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var entry in schedule)
            {
                if (entry.Weekday != date.DayOfWeek)
                {
                    continue;
                }

                var localDateTime = date.ToDateTime(entry.StartsAt);
                yield return new DateTimeOffset(localDateTime, lessonTimeZone.GetUtcOffset(localDateTime));
            }
        }
    }

    // Keeps the proposed appointment and its resolved plan together until the calendar write is saved locally.
    private sealed record ScheduledLessonCandidate(
        LessonAddRequest Request,
        Plan Plan);
}
