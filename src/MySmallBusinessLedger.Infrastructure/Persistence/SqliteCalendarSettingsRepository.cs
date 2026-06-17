using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

public sealed class SqliteCalendarSettingsRepository
{
    private const int SingletonId = 1;
    private readonly Func<LedgerDbContext> createContext;

    public SqliteCalendarSettingsRepository(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    public async Task<CalendarSettings?> GetAsync(CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var entity = await context.CalendarSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(settings => settings.Id == SingletonId, cancellationToken);

        return entity is null
            ? null
            : new CalendarSettings(entity.SelectedCalendarId, entity.GoogleAccountEmail);
    }

    public async Task SaveAsync(CalendarSettings settings, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var entity = await context.CalendarSettings
            .SingleOrDefaultAsync(storedSettings => storedSettings.Id == SingletonId, cancellationToken);

        if (entity is null)
        {
            entity = new CalendarSettingsEntity { Id = SingletonId };
            context.CalendarSettings.Add(entity);
        }

        entity.SelectedCalendarId = settings.SelectedCalendarId;
        entity.GoogleAccountEmail = settings.GoogleAccountEmail;

        await context.SaveChangesAsync(cancellationToken);
    }
}
