using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

public sealed class LedgerDatabaseMigrator
{
    private readonly Func<LedgerDbContext> createContext;

    public LedgerDatabaseMigrator(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
