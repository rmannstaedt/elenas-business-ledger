using Microsoft.EntityFrameworkCore.Design;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

public sealed class LedgerDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LedgerDbContext>
{
    public LedgerDbContext CreateDbContext(string[] args)
    {
        var databasePath = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0]
            : Path.Combine(Path.GetTempPath(), "MySmallBusinessLedger-design-time.db");

        return new LedgerDbContext(SqliteLedgerDbContextFactory.CreateOptions(databasePath));
    }
}
