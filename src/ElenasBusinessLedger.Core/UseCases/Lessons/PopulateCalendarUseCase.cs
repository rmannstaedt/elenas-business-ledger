using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

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

    public async Task<OperationResult<PopulateCalendarResult>> ExecuteAsync(
        PopulateCalendarRequest request,
        CancellationToken cancellationToken)
    {
        var firstOfDisplayMonth = new DateOnly(request.DisplayMonth.Year, request.DisplayMonth.Month, 1);
        var student = await studentRepository.GetByShortNameAsync(request.StudentShortName, cancellationToken);
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
        var localLessons = await lessonRepository.ListForStudentMonthAsync(studentShortName, firstOfDisplayMonth, cancellationToken);
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

        var populate = await calendarGateway.PopulateLessonsAsync(
            requestsToCreate.Select(candidate => candidate.Request).ToArray(),
            firstOfDisplayMonth,
            cancellationToken);
        if (!populate.Succeeded)
        {
            if (populate.Value is { Count: > 0 })
            {
                var saveFailure = await TrySaveCreatedLessonsAsync(
                    student.Id,
                    requestsToCreate,
                    populate.Value,
                    cancellationToken);
                var errors = new List<ValidationError>(populate.Errors);
                if (saveFailure is not null)
                {
                    errors.Add(saveFailure);
                }

                return OperationResult<PopulateCalendarResult>.Failure(
                    new PopulateCalendarResult(
                        requestsToCreate.Length,
                        skippedDuplicates,
                        populate.Value),
                    errors.ToArray());
            }

            return OperationResult<PopulateCalendarResult>.Failure(populate.Errors.ToArray());
        }

        var localSaveFailure = await TrySaveCreatedLessonsAsync(
            student.Id,
            requestsToCreate,
            populate.Value!,
            cancellationToken);
        if (localSaveFailure is not null)
        {
            return OperationResult<PopulateCalendarResult>.Failure(
                new PopulateCalendarResult(
                    requestsToCreate.Length,
                    skippedDuplicates,
                    populate.Value!),
                localSaveFailure);
        }

        return OperationResult<PopulateCalendarResult>.Success(new PopulateCalendarResult(
            requestsToCreate.Length,
            skippedDuplicates,
            populate.Value!));
    }

    private async Task<ValidationError?> TrySaveCreatedLessonsAsync(
        int studentId,
        IReadOnlyList<ScheduledLessonCandidate> candidates,
        IReadOnlyList<CalendarLesson> refreshedLessons,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var candidate in candidates)
            {
                var createdLesson = refreshedLessons.FirstOrDefault(lesson =>
                    string.Equals(
                        lesson.StudentShortName,
                        candidate.Request.StudentShortName,
                        StringComparison.OrdinalIgnoreCase) &&
                    lesson.StartsAt == candidate.Request.StartsAt &&
                    lesson.HasKnownStartTime == candidate.Request.HasKnownStartTime);
                if (createdLesson is null)
                {
                    return new ValidationError(
                        "Lesson.LocalSaveFailed",
                        "Google Calendar was populated, but the created lesson could not be matched for local billing ledger save. Refresh Calendar and check the lesson list before invoicing.");
                }

                await lessonRepository.SaveAsync(
                    new LessonRecord(
                        Id: 0,
                        StudentId: studentId,
                        StudentShortName: candidate.Request.StudentShortName,
                        PlanCatalogId: candidate.Plan.PlanCatalogId,
                        PlanName: candidate.Plan.Name,
                        StartsAt: candidate.Request.StartsAt,
                        DurationMinutes: candidate.Plan.DurationMinutes,
                        State: createdLesson.State,
                        GoogleCalendarEventId: createdLesson.Id,
                        InvoiceNumber: null,
                        HasKnownStartTime: candidate.Request.HasKnownStartTime),
                    cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ValidationError(
                "Lesson.LocalSaveFailed",
                "Google Calendar was populated, but the local billing ledger could not be updated. Refresh Calendar and check the lesson list before invoicing.");
        }

        return null;
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

    private sealed record ScheduledLessonCandidate(
        LessonAddRequest Request,
        Plan Plan);
}
