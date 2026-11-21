using System.Globalization;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace ElenasBusinessLedger.Infrastructure.Persistence;

public sealed class SqliteLessonRepository : ILessonRepository
{
    private readonly Func<LedgerDbContext> createContext;

    public SqliteLessonRepository(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    public async Task<LessonRecord> SaveAsync(LessonRecord lesson, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var entity = lesson.Id > 0
            ? await context.Lessons.Include(storedLesson => storedLesson.Student)
                .SingleOrDefaultAsync(storedLesson => storedLesson.Id == lesson.Id, cancellationToken)
            : null;

        if (entity is null)
        {
            if (lesson.Id > 0)
            {
                throw new InvalidOperationException("The lesson was removed before it could be updated. Refresh the dashboard.");
            }

            entity = new LessonEntity();
            context.Lessons.Add(entity);
        }

        if (entity.Id > 0 && await context.IssuedInvoiceLines.AnyAsync(
            line => line.LessonId == entity.Id && line.Invoice.IssueStatus == InvoiceIssueStatus.Issued, cancellationToken))
        {
            return ToDomain(entity);
        }

        entity.StudentId = lesson.StudentId;
        entity.PlanCatalogId = lesson.PlanCatalogId;
        entity.PlanName = lesson.PlanName;
        entity.StartsAt = lesson.StartsAt;
        entity.HasKnownStartTime = lesson.HasKnownStartTime;
        entity.DurationMinutes = lesson.DurationMinutes;
        entity.State = lesson.State;
        entity.GoogleCalendarEventId = lesson.GoogleCalendarEventId;
        entity.InvoiceNumber = lesson.InvoiceNumber?.Value;

        await context.SaveChangesAsync(cancellationToken);

        var saved = await context.Lessons
            .AsNoTracking()
            .Include(storedLesson => storedLesson.Student)
            .SingleAsync(storedLesson => storedLesson.Id == entity.Id, cancellationToken);

        return ToDomain(saved);
    }

    public async Task<IReadOnlyList<LessonRecord>> ListForStudentMonthAsync(
        string studentShortName,
        DateOnly month,
        CancellationToken cancellationToken)
    {
        var firstDay = new DateOnly(month.Year, month.Month, 1);
        var from = firstDay.ToDateTime(TimeOnly.MinValue);
        var to = firstDay.AddMonths(1).ToDateTime(TimeOnly.MinValue);
        var fromText = FormatSqliteDatePrefix(firstDay);
        var toExclusiveText = FormatSqliteDatePrefix(firstDay.AddMonths(1));

        await using var context = createContext();
        var lessons = await context.Lessons
            .FromSql($"""SELECT * FROM "Lessons" WHERE "StartsAt" >= {fromText} AND "StartsAt" < {toExclusiveText}""")
            .AsNoTracking()
            .Include(lesson => lesson.Student)
            .Where(lesson => lesson.Student.ShortName == studentShortName)
            .ToArrayAsync(cancellationToken);

        return lessons
            .Where(lesson => lesson.StartsAt.DateTime >= from && lesson.StartsAt.DateTime < to)
            .OrderBy(lesson => lesson.StartsAt)
            .Select(ToDomain)
            .ToArray();
    }

    public async Task<IReadOnlyList<LessonRecord>> ListForWindowAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var fromDate = from.ToDateTime(TimeOnly.MinValue);
        var toExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var fromText = FormatSqliteDatePrefix(from);
        var toExclusiveText = FormatSqliteDatePrefix(to.AddDays(1));

        await using var context = createContext();
        var lessons = await context.Lessons
            .FromSql($"""SELECT * FROM "Lessons" WHERE "StartsAt" >= {fromText} AND "StartsAt" < {toExclusiveText}""")
            .AsNoTracking()
            .Include(lesson => lesson.Student)
            .ToArrayAsync(cancellationToken);

        return lessons
            .Where(lesson => lesson.StartsAt.DateTime >= fromDate && lesson.StartsAt.DateTime < toExclusive)
            .OrderBy(lesson => lesson.StartsAt)
            .Select(ToDomain)
            .ToArray();
    }

    public async Task<LessonRecord?> UpdatePlanAsync(
        int lessonId,
        int planCatalogId,
        string planName,
        int durationMinutes,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var lesson = await context.Lessons
            .Include(storedLesson => storedLesson.Student)
            .SingleOrDefaultAsync(storedLesson => storedLesson.Id == lessonId, cancellationToken);
        if (lesson is null || lesson.InvoiceNumber is not null)
        {
            return null;
        }

        lesson.PlanCatalogId = planCatalogId;
        lesson.PlanName = planName;
        lesson.DurationMinutes = durationMinutes;

        await context.SaveChangesAsync(cancellationToken);

        return ToDomain(lesson);
    }

    public async Task<LessonRecord?> UpdateUninvoicedTimingByGoogleCalendarEventIdAsync(
        string googleCalendarEventId,
        DateTimeOffset startsAt,
        bool hasKnownStartTime,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var lesson = await context.Lessons
            .Include(storedLesson => storedLesson.Student)
            .SingleOrDefaultAsync(
                storedLesson => storedLesson.GoogleCalendarEventId == googleCalendarEventId &&
                                storedLesson.InvoiceNumber == null,
                cancellationToken);
        if (lesson is null)
        {
            return null;
        }

        lesson.StartsAt = startsAt;
        lesson.HasKnownStartTime = hasKnownStartTime;

        await context.SaveChangesAsync(cancellationToken);

        return ToDomain(lesson);
    }

    public async Task<LessonRecord?> UpdateUninvoicedStateByGoogleCalendarEventIdAsync(
        string googleCalendarEventId,
        LessonState state,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var lesson = await context.Lessons
            .Include(storedLesson => storedLesson.Student)
            .SingleOrDefaultAsync(
                storedLesson => storedLesson.GoogleCalendarEventId == googleCalendarEventId &&
                                storedLesson.InvoiceNumber == null,
                cancellationToken);
        if (lesson is null)
        {
            return null;
        }

        lesson.State = state;

        await context.SaveChangesAsync(cancellationToken);

        return ToDomain(lesson);
    }

    public async Task<bool> DeleteUninvoicedByGoogleCalendarEventIdAsync(
        string googleCalendarEventId,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var lesson = await context.Lessons
            .SingleOrDefaultAsync(
                storedLesson => storedLesson.GoogleCalendarEventId == googleCalendarEventId &&
                                storedLesson.InvoiceNumber == null,
                cancellationToken);
        if (lesson is null)
        {
            return false;
        }

        context.Lessons.Remove(lesson);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<LessonRecord>> ListByGoogleCalendarEventIdsAsync(
        IReadOnlyList<string> eventIds, CancellationToken cancellationToken)
    {
        if (eventIds.Count == 0)
        {
            return [];
        }

        await using var context = createContext();
        var lessons = await context.Lessons.AsNoTracking()
            .Include(lesson => lesson.Student)
            .Where(lesson => lesson.GoogleCalendarEventId != null && eventIds.Contains(lesson.GoogleCalendarEventId))
            .ToArrayAsync(cancellationToken);
        return lessons.Select(ToDomain).ToArray();
    }

    private static LessonRecord ToDomain(LessonEntity entity) =>
        new(
            entity.Id,
            entity.StudentId,
            entity.Student.ShortName,
            entity.PlanCatalogId,
            entity.PlanName,
            entity.StartsAt,
            entity.DurationMinutes,
            entity.State,
            entity.GoogleCalendarEventId,
            entity.InvoiceNumber is null ? null : new InvoiceNumber(entity.InvoiceNumber.Value),
            entity.HasKnownStartTime);

    private static string FormatSqliteDatePrefix(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
