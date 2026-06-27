using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

public sealed class LedgerDatabaseMigrator
{
    private const string MigrationWarningsTableName = "__LedgerMigrationWarnings";

    private readonly Func<LedgerDbContext> createContext;

    public LedgerDatabaseMigrator(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    public async Task<IReadOnlyList<LedgerMigrationWarning>> MigrateAsync(CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await context.Database.MigrateAsync(cancellationToken);
        return await ReadAndClearMigrationWarningsAsync(context, cancellationToken);
    }

    private static async Task<IReadOnlyList<LedgerMigrationWarning>> ReadAndClearMigrationWarningsAsync(
        LedgerDbContext context,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            if (!await MigrationWarningsTableExistsAsync(connection, cancellationToken))
            {
                return Array.Empty<LedgerMigrationWarning>();
            }

            var warnings = new List<LedgerMigrationWarning>();
            await using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    $"""
                    SELECT "Title", "Message"
                    FROM "{MigrationWarningsTableName}"
                    ORDER BY "Id";
                    """;
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    warnings.Add(new LedgerMigrationWarning(reader.GetString(0), reader.GetString(1)));
                }
            }

            if (warnings.Count > 0)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $"""DELETE FROM "{MigrationWarningsTableName}";""";
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            return warnings;
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> MigrationWarningsTableExistsAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'table'
              AND name = '__LedgerMigrationWarnings';
            """;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }
}
