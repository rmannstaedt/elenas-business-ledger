using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Lessons;

public sealed class PopulateCalendarUseCase
{
    private readonly IStudentRepository studentRepository;
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly TimeZoneInfo lessonTimeZone;

    public PopulateCalendarUseCase(
        IStudentRepository studentRepository,
        IGoogleCalendarGateway calendarGateway,
        TimeZoneInfo lessonTimeZone)
    {
        this.studentRepository = studentRepository;
        this.calendarGateway = calendarGateway;
        this.lessonTimeZone = lessonTimeZone;
    }

    public async Task<OperationResult<PopulateCalendarResult>> ExecuteAsync(
        PopulateCalendarRequest request,
        CancellationToken cancellationToken)
    {
        var firstOfDisplayMonth = new DateOnly(request.DisplayMonth.Year, request.DisplayMonth.Month, 1);
        var firstOfCurrentMonth = new DateOnly(request.Today.Year, request.Today.Month, 1);
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

        var from = firstOfDisplayMonth == firstOfCurrentMonth ? request.Today : firstOfDisplayMonth;
        var to = firstOfDisplayMonth.AddMonths(1).AddDays(-1);
        var existingLessons = await calendarGateway.ListLessonsAsync(from, to, cancellationToken);
        var existingStarts = existingLessons
            .Where(lesson => string.Equals(lesson.StudentShortName, request.StudentShortName, StringComparison.OrdinalIgnoreCase))
            .Select(lesson => lesson.StartsAt)
            .ToHashSet();

        var candidates = CreateScheduledStarts(student.Schedule, from, to)
            .Select(startsAt => new
            {
                StartsAt = startsAt,
                Plan = MySmallBusinessLedger.Core.Plans.StudentPlanPeriod.GetActivePlan(
                    student.PlanPeriods,
                    DateOnly.FromDateTime(startsAt.DateTime))
            })
            .Where(candidate => candidate.Plan is not null)
            .Select(candidate => new LessonAddRequest(
                request.StudentShortName,
                candidate.StartsAt,
                candidate.Plan!.PlanCatalogId))
            .ToArray();
        var requestsToCreate = candidates
            .Where(candidate => !existingStarts.Contains(candidate.StartsAt))
            .ToArray();
        var skippedDuplicates = candidates.Length - requestsToCreate.Length;

        if (requestsToCreate.Length == 0)
        {
            return OperationResult<PopulateCalendarResult>.Success(new PopulateCalendarResult(
                CreatedCount: 0,
                skippedDuplicates,
                existingLessons));
        }

        var populate = await calendarGateway.PopulateLessonsAsync(requestsToCreate, firstOfDisplayMonth, cancellationToken);
        if (!populate.Succeeded)
        {
            return OperationResult<PopulateCalendarResult>.Failure(populate.Errors.ToArray());
        }

        return OperationResult<PopulateCalendarResult>.Success(new PopulateCalendarResult(
            requestsToCreate.Length,
            skippedDuplicates,
            populate.Value!));
    }

    private IEnumerable<DateTimeOffset> CreateScheduledStarts(
        IReadOnlyList<MySmallBusinessLedger.Core.Students.StudentScheduleEntry> schedule,
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
}
