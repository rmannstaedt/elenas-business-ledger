using System.Data;
using System.Data.Common;
using System.Globalization;
using ElenasBusinessLedger.Core.Students;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ElenasBusinessLedger.Infrastructure.Persistence;

// Backs up existing data before applying EF migrations and reports historical issues that need attention.
public sealed class LedgerDatabaseMigrator
{
    private const string MigrationWarningsTableName = "__LedgerMigrationWarnings";

    private readonly Func<LedgerDbContext> createContext;

    public LedgerDatabaseMigrator(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    // Backs up an existing ledger before a schema upgrade and returns integrity warnings for startup to display.
    public async Task<IReadOnlyList<LedgerMigrationWarning>> MigrateAsync(CancellationToken cancellationToken)
    {
        await using var context = createContext();
        await BackUpBeforeUpgradeAsync(context, cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var warnings = (await ReadAndClearMigrationWarningsAsync(context, cancellationToken)).ToList();
        var numbers = await context.IssuedInvoices.AsNoTracking()
            .OrderBy(invoice => invoice.InvoiceNumber).Select(invoice => invoice.InvoiceNumber)
            .ToArrayAsync(cancellationToken);
        var previous = 0L;
        var gaps = new List<string>();
        foreach (var number in numbers)
        {
            if (number > previous + 1)
            {
                gaps.Add(number == previous + 2 ? $"{previous + 1:D3}" : $"{previous + 1:D3}–{number - 1:D3}");
            }
            previous = number;
        }

        if (gaps.Count > 0)
        {
            warnings.Add(new LedgerMigrationWarning("Historical invoice numbering gaps",
                $"Missing invoice numbers in the existing ledger: {string.Join(", ", gaps)}. Existing invoices were preserved."));
        }

        var shortNames = await context.Students.AsNoTracking().Select(student => student.ShortName).ToArrayAsync(cancellationToken);
        var ambiguous = shortNames.GroupBy(StudentShortNameKey.From).Where(group => group.Count() > 1)
            .Select(group => string.Join(" / ", group.Order(StringComparer.Ordinal))).ToArray();
        if (ambiguous.Length > 0)
        {
            warnings.Add(new LedgerMigrationWarning("Conflicting student short names",
                $"Rename duplicate short names in Manage Students so calendar titles identify one student. Conflicts: {string.Join(", ", ambiguous)}."));
        }

        return warnings;
    }

    private static async Task BackUpBeforeUpgradeAsync(LedgerDbContext context, CancellationToken cancellationToken)
    {
        var databasePath = context.Database.GetDbConnection().DataSource;
        if (!File.Exists(databasePath) ||
            !(await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            return;
        }

        var backupDirectory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(databasePath))!, "Backups");
        Directory.CreateDirectory(backupDirectory);
        var backupId = Guid.NewGuid().ToString("N")[..8];
        var fileName = $"ledger-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{backupId}.db";
        var backupPath = Path.Combine(backupDirectory, fileName);
        var partialPath = backupPath + ".partial";

        try
        {
            await using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString()))
            await using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = partialPath,
                    Pooling = false
                }.ToString()))
            {
                await source.OpenAsync(cancellationToken);
                await destination.OpenAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                source.BackupDatabase(destination);

                await using var check = destination.CreateCommand();
                check.CommandText = "PRAGMA quick_check;";
                if (!string.Equals(await check.ExecuteScalarAsync(cancellationToken) as string, "ok", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The pre-upgrade ledger backup failed its integrity check.");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(partialPath, backupPath, overwrite: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
        {
            throw new InvalidOperationException(
                $"The ledger could not be backed up in '{backupDirectory}'. The database upgrade has not started. {ex.Message}", ex);
        }
        finally
        {
            if (File.Exists(partialPath))
            {
                File.Delete(partialPath);
            }
        }
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
