using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

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
            ? await context.Lessons.SingleOrDefaultAsync(storedLesson => storedLesson.Id == lesson.Id, cancellationToken)
            : null;

        if (entity is null)
        {
            entity = new LessonEntity();
            context.Lessons.Add(entity);
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

        await using var context = createContext();
        var lessons = await context.Lessons
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

        await using var context = createContext();
        var lessons = await context.Lessons
            .AsNoTracking()
            .Include(lesson => lesson.Student)
            .ToArrayAsync(cancellationToken);

        return lessons
            .Where(lesson => lesson.StartsAt.DateTime >= fromDate && lesson.StartsAt.DateTime < toExclusive)
            .OrderBy(lesson => lesson.StartsAt)
            .Select(ToDomain)
            .ToArray();
    }

    public async Task MarkInvoicedAsync(
        IReadOnlyList<int> lessonIds,
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken)
    {
        if (lessonIds.Count == 0)
        {
            return;
        }

        await using var context = createContext();
        var lessons = await context.Lessons
            .Where(lesson => lessonIds.Contains(lesson.Id))
            .ToArrayAsync(cancellationToken);

        foreach (var lesson in lessons)
        {
            lesson.InvoiceNumber = invoiceNumber.Value;
        }

        await context.SaveChangesAsync(cancellationToken);
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

    public async Task DeleteUninvoicedAsync(IReadOnlyList<int> lessonIds, CancellationToken cancellationToken)
    {
        if (lessonIds.Count == 0)
        {
            return;
        }

        await using var context = createContext();
        var lessons = await context.Lessons
            .Where(lesson => lessonIds.Contains(lesson.Id))
            .Where(lesson => lesson.InvoiceNumber == null)
            .ToArrayAsync(cancellationToken);

        context.Lessons.RemoveRange(lessons);
        await context.SaveChangesAsync(cancellationToken);
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
}
