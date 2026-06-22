using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

public static class SqliteLedgerDbContextFactory
{
    public static Func<LedgerDbContext> Create(string databasePath, bool pooling = true)
    {
        var options = CreateOptions(databasePath, pooling);
        return () => new LedgerDbContext(options);
    }

    public static DbContextOptions<LedgerDbContext> CreateOptions(string databasePath, bool pooling = true)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = pooling
        }.ToString();
        return new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite(connectionString)
            .Options;
    }
}
