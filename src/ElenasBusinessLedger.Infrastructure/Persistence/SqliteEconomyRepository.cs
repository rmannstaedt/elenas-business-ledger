using Microsoft.EntityFrameworkCore;
using ElenasBusinessLedger.Core.Money;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Infrastructure.Persistence;

// Stores the report cutover and manual legacy amounts so Economy can reload them consistently.
public sealed class SqliteEconomyRepository : IEconomyRepository
{
    private const int SingletonSettingsId = 1;
    private const string DkkCurrency = "DKK";
    private readonly Func<LedgerDbContext> createContext;

    public SqliteEconomyRepository(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    public async Task<EconomySettings?> GetSettingsAsync(CancellationToken cancellationToken)
    {
        await using var context = createContext();
        return await ReadSettingsAsync(context, cancellationToken);
    }

    internal static async Task<EconomySettings?> ReadSettingsAsync(LedgerDbContext context, CancellationToken cancellationToken)
    {
        var settings = await context.EconomySettings
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == SingletonSettingsId, cancellationToken);
        return settings is null
            ? null
            : new EconomySettings(NormalizeMonth(settings.FirstApplicationInvoiceMonth));
    }

    public async Task<IReadOnlyList<EconomyManualMonthOverride>> ListManualMonthOverridesAsync(
        int fromYearInclusive,
        int toYearInclusive,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        return await context.EconomyManualMonthOverrides
            .AsNoTracking()
            .Where(manual => manual.Year >= fromYearInclusive && manual.Year <= toYearInclusive)
            .OrderBy(manual => manual.Year)
            .ThenBy(manual => manual.Month)
            .Select(manual => new EconomyManualMonthOverride(
                manual.Year,
                manual.Month,
                Money.Dkk(manual.NetAmount),
                Money.Dkk(manual.VatAmount)))
            .ToArrayAsync(cancellationToken);
    }

    // Saves the first app-backed invoice month and removes manual overrides that the new cutover makes obsolete.
    public async Task SaveSettingsAsync(EconomySettings settings, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await SetSettingsAndRemoveObsoleteOverridesAsync(context, settings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    internal static async Task SetSettingsAndRemoveObsoleteOverridesAsync(
        LedgerDbContext context, EconomySettings settings, CancellationToken cancellationToken)
    {
        await SaveSettingsAsync(context, settings, cancellationToken);
        await RemoveManualOverridesOnOrAfterCutOverAsync(context, settings.FirstApplicationInvoiceMonth, cancellationToken);
    }

    // Saves the manual amounts and removes entries on or after the saved cutover.
    public async Task SaveManualMonthOverridesAsync(
        IReadOnlyList<EconomyManualMonthOverride> overrides,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var cutOver = await GetSavedCutOverAsync(context, cancellationToken);
        if (cutOver is null && overrides.Any(HasNonZeroAmount))
        {
            throw new InvalidOperationException("Manual economy overrides require a saved economy cut-over month.");
        }

        await SaveManualMonthOverridesAsync(context, overrides, cancellationToken);
        if (cutOver is null)
        {
            await RemoveAllManualOverridesAsync(context, cancellationToken);
        }
        else
        {
            await RemoveManualOverridesOnOrAfterCutOverAsync(context, cutOver.Value, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    // Commits a new cutover and manual amounts together so reporting sees a consistent set of settings.
    public async Task SaveSettingsAndManualMonthOverridesAsync(
        EconomySettings settings,
        IReadOnlyList<EconomyManualMonthOverride> overrides,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await SaveSettingsAsync(context, settings, cancellationToken);
        await SaveManualMonthOverridesAsync(context, overrides, cancellationToken);
        await RemoveManualOverridesOnOrAfterCutOverAsync(context, settings.FirstApplicationInvoiceMonth, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task SaveSettingsAsync(
        LedgerDbContext context,
        EconomySettings settings,
        CancellationToken cancellationToken)
    {
        var entity = await context.EconomySettings.SingleOrDefaultAsync(
            stored => stored.Id == SingletonSettingsId,
            cancellationToken);
        if (entity is null)
        {
            context.EconomySettings.Add(new EconomySettingsEntity
            {
                Id = SingletonSettingsId,
                FirstApplicationInvoiceMonth = NormalizeMonth(settings.FirstApplicationInvoiceMonth)
            });
        }
        else
        {
            entity.FirstApplicationInvoiceMonth = NormalizeMonth(settings.FirstApplicationInvoiceMonth);
        }
    }

    private static async Task SaveManualMonthOverridesAsync(
        LedgerDbContext context,
        IReadOnlyList<EconomyManualMonthOverride> overrides,
        CancellationToken cancellationToken)
    {
        foreach (var manual in overrides)
        {
            ValidateManualOverride(manual);
            var existing = await context.EconomyManualMonthOverrides.SingleOrDefaultAsync(
                stored => stored.Year == manual.Year && stored.Month == manual.Month,
                cancellationToken);
            if (manual.Net.Amount == 0m && manual.Vat.Amount == 0m)
            {
                if (existing is not null)
                {
                    context.EconomyManualMonthOverrides.Remove(existing);
                }

                continue;
            }

            var now = DateTimeOffset.UtcNow;
            if (existing is null)
            {
                context.EconomyManualMonthOverrides.Add(new EconomyManualMonthOverrideEntity
                {
                    Year = manual.Year,
                    Month = manual.Month,
                    NetAmount = manual.Net.Amount,
                    VatAmount = manual.Vat.Amount,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                existing.NetAmount = manual.Net.Amount;
                existing.VatAmount = manual.Vat.Amount;
                existing.UpdatedAt = now;
            }
        }
    }

    private static DateOnly NormalizeMonth(DateOnly month) =>
        new(month.Year, month.Month, 1);

    private static async Task<DateOnly?> GetSavedCutOverAsync(
        LedgerDbContext context,
        CancellationToken cancellationToken)
    {
        var settings = await context.EconomySettings
            .AsNoTracking()
            .SingleOrDefaultAsync(stored => stored.Id == SingletonSettingsId, cancellationToken);

        return settings is null
            ? null
            : NormalizeMonth(settings.FirstApplicationInvoiceMonth);
    }

    private static async Task RemoveAllManualOverridesAsync(
        LedgerDbContext context,
        CancellationToken cancellationToken)
    {
        var manualOverrides = await context.EconomyManualMonthOverrides.ToArrayAsync(cancellationToken);
        context.EconomyManualMonthOverrides.RemoveRange(manualOverrides);

        var addedManualOverrides = context.ChangeTracker
            .Entries<EconomyManualMonthOverrideEntity>()
            .Where(entry => entry.State == EntityState.Added)
            .ToArray();
        foreach (var entry in addedManualOverrides)
        {
            entry.State = EntityState.Detached;
        }
    }

    private static async Task RemoveManualOverridesOnOrAfterCutOverAsync(
        LedgerDbContext context,
        DateOnly firstApplicationInvoiceMonth,
        CancellationToken cancellationToken)
    {
        var cutOver = NormalizeMonth(firstApplicationInvoiceMonth);
        var manualOverrides = await context.EconomyManualMonthOverrides
            .Where(stored =>
                stored.Year > cutOver.Year ||
                stored.Year == cutOver.Year && stored.Month >= cutOver.Month)
            .ToArrayAsync(cancellationToken);

        context.EconomyManualMonthOverrides.RemoveRange(manualOverrides);

        var addedManualOverrides = context.ChangeTracker
            .Entries<EconomyManualMonthOverrideEntity>()
            .Where(entry =>
                entry.State == EntityState.Added &&
                IsOnOrAfterCutOver(entry.Entity, cutOver))
            .ToArray();
        foreach (var entry in addedManualOverrides)
        {
            entry.State = EntityState.Detached;
        }
    }

    private static bool IsOnOrAfterCutOver(EconomyManualMonthOverrideEntity manual, DateOnly cutOver) =>
        manual.Year > cutOver.Year ||
        manual.Year == cutOver.Year && manual.Month >= cutOver.Month;

    private static bool HasNonZeroAmount(EconomyManualMonthOverride manual) =>
        manual.Net.Amount != 0m || manual.Vat.Amount != 0m;

    private static void ValidateManualOverride(EconomyManualMonthOverride manual)
    {
        if (manual.Month is < 1 or > 12)
        {
            throw new InvalidOperationException("Manual economy override month must be between 1 and 12.");
        }

        if (!StringComparer.Ordinal.Equals(manual.Net.Currency, DkkCurrency) ||
            !StringComparer.Ordinal.Equals(manual.Vat.Currency, DkkCurrency))
        {
            throw new InvalidOperationException("Manual economy overrides must use DKK.");
        }
    }
}
