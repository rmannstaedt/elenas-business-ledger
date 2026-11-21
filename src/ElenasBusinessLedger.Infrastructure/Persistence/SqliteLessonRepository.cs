using System.Globalization;
using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace ElenasBusinessLedger.Infrastructure.Persistence;

// Persists local lessons and calendar holds so appointment changes and invoice reservations coordinate in SQLite.
public sealed class SqliteLessonRepository : ILessonRepository
{
    private readonly Func<LedgerDbContext> createContext;

    public SqliteLessonRepository(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    // Places a hold before a calendar write so invoice reservation and the calendar change cannot claim the same lesson.
    public async Task<OperationResult<bool>> ClaimCalendarMutationAsync(
        string eventId, CalendarMutationKind kind, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var lesson = await context.Lessons.SingleOrDefaultAsync(candidate => candidate.GoogleCalendarEventId == eventId, cancellationToken);
        if (lesson is null)
        {
            return OperationResult<bool>.Failure(new ValidationError("Lesson.NotFound", "Refresh the dashboard before changing this lesson."));
        }

        if (lesson.PendingCalendarMutation is not null)
        {
            return OperationResult<bool>.Failure(new ValidationError("Lesson.CalendarChangePending",
                "A calendar change for this lesson needs review. Check the appointment before making another change."));
        }

        if (lesson.InvoiceNumber is not null || await context.IssuedInvoiceLines.AnyAsync(line => line.LessonId == lesson.Id, cancellationToken))
        {
            return OperationResult<bool>.Failure(new ValidationError("Lesson.InvoiceReservation",
                "This lesson belongs to an issued or pending invoice. Resolve any pending invoice before changing it."));
        }

        lesson.PendingCalendarMutation = kind;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    // Removes the matching hold when a calendar change was rejected and billing can use the existing row again.
    public async Task ClearCalendarMutationAsync(string eventId, CalendarMutationKind kind, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await context.Lessons.Where(lesson => lesson.GoogleCalendarEventId == eventId && lesson.PendingCalendarMutation == kind)
            .ExecuteUpdateAsync(setters => setters.SetProperty(lesson => lesson.PendingCalendarMutation, (CalendarMutationKind?)null),
                cancellationToken);
    }

    // Applies an observed appointment or confirmed removal to release a pending calendar hold.
    public async Task<OperationResult<bool>> ResolveCalendarMutationAsync(
        string eventId, CalendarLesson? observed, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var lesson = await context.Lessons.SingleOrDefaultAsync(candidate => candidate.GoogleCalendarEventId == eventId, cancellationToken);
        if (lesson is null || lesson.PendingCalendarMutation is null || lesson.InvoiceNumber is not null ||
            await context.IssuedInvoiceLines.AnyAsync(line => line.LessonId == lesson.Id, cancellationToken))
        {
            return OperationResult<bool>.Failure(new ValidationError("Lesson.ReviewChanged",
                "The lesson changed before review could finish. Refresh the dashboard."));
        }

        if (observed is null)
        {
            context.Lessons.Remove(lesson);
        }
        else
        {
            lesson.StartsAt = observed.StartsAt;
            lesson.HasKnownStartTime = observed.HasKnownStartTime;
            lesson.State = observed.State;
            lesson.PendingCalendarMutation = null;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    // Writes a local billing row while preserving any existing lesson held by invoicing or calendar review.
    public async Task<LessonRecord> SaveAsync(LessonRecord lesson, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
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

        if (entity.Id > 0 && (entity.PendingCalendarMutation is not null ||
            await context.IssuedInvoiceLines.AnyAsync(line => line.LessonId == entity.Id, cancellationToken)))
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

        await transaction.CommitAsync(cancellationToken);
        return ToDomain(saved);
    }

    // Reads one student's lessons in the local calendar month for previews and schedule checks.
    public async Task<IReadOnlyList<LessonRecord>> ListForStudentMonthAsync(
        string studentShortName,
        DateOnly month,
        CancellationToken cancellationToken,
        int? studentId = null)
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
            .Where(lesson => studentId == null ? lesson.Student.ShortName == studentShortName : lesson.StudentId == studentId)
            .ToArrayAsync(cancellationToken);

        return lessons
            .Where(lesson => lesson.StartsAt.DateTime >= from && lesson.StartsAt.DateTime < to)
            .OrderBy(lesson => lesson.StartsAt)
            .Select(ToDomain)
            .ToArray();
    }

    // Loads local lesson dates across a window for dashboard reconciliation and billing selection.
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

    // Updates a lesson's plan if neither invoicing nor a pending calendar mutation has locked it.
    public async Task<LessonRecord?> UpdatePlanAsync(
        int lessonId,
        int planCatalogId,
        string planName,
        int durationMinutes,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var lesson = await context.Lessons
            .Include(storedLesson => storedLesson.Student)
            .SingleOrDefaultAsync(storedLesson => storedLesson.Id == lessonId, cancellationToken);
        if (lesson is null || lesson.InvoiceNumber is not null || lesson.PendingCalendarMutation is not null ||
            await context.IssuedInvoiceLines.AnyAsync(line => line.LessonId == lessonId, cancellationToken))
        {
            return null;
        }

        lesson.PlanCatalogId = planCatalogId;
        lesson.PlanName = planName;
        lesson.DurationMinutes = durationMinutes;

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDomain(lesson);
    }

    // Records an acknowledged move and clears the billing hold on the matched lesson.
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
        lesson.PendingCalendarMutation = null;

        await context.SaveChangesAsync(cancellationToken);

        return ToDomain(lesson);
    }

    // Records the acknowledged completion or reopening and clears the lesson's billing hold.
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
        lesson.PendingCalendarMutation = null;

        await context.SaveChangesAsync(cancellationToken);

        return ToDomain(lesson);
    }

    // Removes the matched local billing row after Google Calendar acknowledges removal.
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

    // Finds saved links by exact event ID so synchronization can reuse the established student ownership.
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
            entity.HasKnownStartTime,
            entity.PendingCalendarMutation);

    private static string FormatSqliteDatePrefix(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
