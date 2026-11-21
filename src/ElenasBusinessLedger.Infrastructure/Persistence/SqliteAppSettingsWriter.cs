using ElenasBusinessLedger.Core.Company;
using ElenasBusinessLedger.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace ElenasBusinessLedger.Infrastructure.Persistence;

public sealed class SqliteAppSettingsWriter(Func<LedgerDbContext> createContext)
{
    public async Task<EconomySettings?> SaveAsync(
        CompanyProfile company,
        CalendarSettings calendar,
        EconomySettings? economy,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await SqliteCompanyProfileRepository.SetAsync(context, company, cancellationToken);
        await SqliteCalendarSettingsRepository.SetAsync(context, calendar, cancellationToken);
        if (economy is not null)
        {
            await SqliteEconomyRepository.SetSettingsAndRemoveObsoleteOverridesAsync(context, economy, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        var savedEconomySettings = await SqliteEconomyRepository.ReadSettingsAsync(context, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return savedEconomySettings;
    }
}
