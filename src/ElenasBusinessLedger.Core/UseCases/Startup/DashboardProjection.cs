using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Startup;

// Turns reconciled lesson records and invoice facts into dashboard rows and navigation choices.
internal sealed class DashboardProjection(TimeZoneInfo lessonTimeZone)
{
    public static StartupDashboardResult Create(
        StudentRecord? selectedStudent,
        IReadOnlyList<StudentRecord> students,
        IEnumerable<(LessonRecord Record, InvoiceLessonStatus? InvoiceStatus)> lessonSources,
        IReadOnlyList<IssuedInvoiceSummary> invoices,
        IReadOnlyList<DashboardStudentNavigationEntry> navigationEntries,
        IReadOnlyList<DashboardPlanOption> planOptions)
    {
        var lessons = lessonSources
            .OrderBy(source => source.Record.StartsAt)
            .Select(source => CreateDashboardLesson(source.Record, source.InvoiceStatus))
            .ToArray();
        var shortNames = students
            .Select(student => student.Student.ShortName)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var lessonDefaults = selectedStudent is null
            ? null
            : new DashboardStudentLessonDefaults(selectedStudent.Student.ShortName, selectedStudent.Schedule, selectedStudent.PlanPeriods);

        return new StartupDashboardResult(
            selectedStudent?.Student.ShortName,
            shortNames,
            lessons,
            invoices,
            selectedStudent?.Schedule.Count > 0,
            navigationEntries,
            planOptions,
            lessonDefaults,
            selectedStudent is { Id: > 0 } ? selectedStudent.Id : null,
            students.Select(student => new StudentChoice(student.Id, student.Student.ShortName)).ToArray());
    }

    private static DashboardLesson CreateDashboardLesson(
        LessonRecord record,
        InvoiceLessonStatus? invoiceStatus)
    {
        var invoiceNumber = invoiceStatus?.InvoiceNumber;
        var isInvoiced = invoiceNumber is not null;

        return new(
            record.GoogleCalendarEventId ?? record.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            record.StudentShortName,
            record.StartsAt,
            isInvoiced ? LessonState.Completed : record.State,
            isInvoiced,
            invoiceStatus?.PaymentStatus,
            invoiceNumber,
            record.PlanCatalogId,
            record.PlanName,
            record.Id,
            record.HasKnownStartTime,
            record.StudentId > 0 ? record.StudentId : null,
            record.PendingCalendarMutation is not null,
            record.PlanCatalogId <= 0);
    }

    public IReadOnlyList<DashboardStudentNavigationEntry> CreateStudentNavigationEntries(
        DateOnly selectedMonth,
        IReadOnlyList<CalendarLesson> calendarLessons,
        IReadOnlyList<StudentRecord> students)
    {
        var fallbackFrom = selectedMonth.WithDay(1);
        var fallbackTo = fallbackFrom.AddMonths(1).AddDays(-1);
        var navigationFrom = selectedMonth.AddMonths(-1).WithDay(1);
        var navigationTo = selectedMonth.AddMonths(2).WithDay(1).AddDays(-1);
        var activeStudentShortNames = students
            .Select(student => student.Student.ShortName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var scheduledEntries = calendarLessons
            .Where(lesson => activeStudentShortNames.Contains(lesson.StudentShortName))
            .Where(lesson => IsInDateWindow(lesson.StartsAt, navigationFrom, navigationTo))
            .Select(lesson => new DashboardStudentNavigationEntry(lesson.StudentShortName, lesson.StartsAt, StudentId: lesson.StudentId))
            .OrderBy(entry => entry.StartsAt)
            .ThenBy(entry => entry.StudentShortName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (scheduledEntries.Length > 0)
        {
            return scheduledEntries;
        }

        var defaultEntries = students
            .SelectMany(student => CreateScheduledStarts(student.Schedule, fallbackFrom, fallbackTo)
                .Select(startsAt => new DashboardStudentNavigationEntry(
                    student.Student.ShortName,
                    startsAt,
                    HasCalendarLesson: false,
                    StudentId: student.Id > 0 ? student.Id : null)))
            .ToArray();

        return OrderNavigationEntries(defaultEntries);
    }

    private static IReadOnlyList<DashboardStudentNavigationEntry> OrderNavigationEntries(
        IEnumerable<DashboardStudentNavigationEntry> entries) =>
        entries
            .OrderBy(entry => entry.StartsAt)
            .ThenBy(entry => entry.StudentShortName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static bool IsInDateWindow(DateTimeOffset startsAt, DateOnly from, DateOnly to)
    {
        var date = DateOnly.FromDateTime(startsAt.DateTime);
        return date >= from && date <= to;
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
}
