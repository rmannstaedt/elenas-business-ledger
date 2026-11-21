using Microsoft.EntityFrameworkCore.Design;

namespace ElenasBusinessLedger.Infrastructure.Persistence;

// Gives EF tooling a context outside WPF startup, with a temporary database path by default.
public sealed class LedgerDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LedgerDbContext>
{
    // Gives EF tooling a database context, defaulting to a temporary database outside the user's ledger.
    public LedgerDbContext CreateDbContext(string[] args)
    {
        var databasePath = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0]
            : Path.Combine(Path.GetTempPath(), "ElenasBusinessLedger-design-time.db");

        return new LedgerDbContext(SqliteLedgerDbContextFactory.CreateOptions(databasePath));
    }
}
