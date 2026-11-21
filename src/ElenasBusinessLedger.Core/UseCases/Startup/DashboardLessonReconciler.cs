using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Startup;

// Links or updates observed appointments in the local ledger while preserving issued lesson records.
internal sealed class DashboardLessonReconciler(ILessonRepository lessonRepository)
{
    // Returns the saved record for the dashboard, or an unsaved appointment when an applicable plan is missing.
    public async Task<LessonRecord> ReconcileAsync(
        CalendarLesson calendarLesson,
        LessonRecord? localLesson,
        bool isInvoiced,
        IReadOnlyList<StudentRecord> students,
        CancellationToken cancellationToken)
    {
        if (localLesson is not null && isInvoiced)
        {
            return localLesson;
        }

        var record = localLesson is null
            ? CreateLocalLesson(calendarLesson, students)
            : localLesson with
            {
                StartsAt = calendarLesson.StartsAt,
                HasKnownStartTime = calendarLesson.HasKnownStartTime,
                State = calendarLesson.State,
                GoogleCalendarEventId = calendarLesson.Id,
                InvoiceNumber = null
            };

        if (record.PlanCatalogId > 0 && record != localLesson)
        {
            record = await lessonRepository.SaveAsync(record, cancellationToken);
        }

        return record;
    }

    private static LessonRecord CreateLocalLesson(
        CalendarLesson calendarLesson,
        IReadOnlyList<StudentRecord> students)
    {
        var student = students.FirstOrDefault(student =>
            calendarLesson.StudentId is { } studentId ? student.Id == studentId :
                string.Equals(student.Student.ShortName, calendarLesson.StudentShortName, StringComparison.Ordinal));
        var lessonDate = DateOnly.FromDateTime(calendarLesson.StartsAt.DateTime);
        var plan = student is null
            ? null
            : StudentPlanPeriod.GetActivePlan(student.PlanPeriods, lessonDate);

        return new LessonRecord(
            Id: 0,
            StudentId: student?.Id ?? 0,
            StudentShortName: calendarLesson.StudentShortName,
            PlanCatalogId: plan?.PlanCatalogId ?? 0,
            PlanName: plan?.Name ?? string.Empty,
            StartsAt: calendarLesson.StartsAt,
            DurationMinutes: plan?.DurationMinutes ?? 60,
            State: calendarLesson.State,
            GoogleCalendarEventId: calendarLesson.Id,
            InvoiceNumber: null,
            HasKnownStartTime: calendarLesson.HasKnownStartTime);
    }
}
