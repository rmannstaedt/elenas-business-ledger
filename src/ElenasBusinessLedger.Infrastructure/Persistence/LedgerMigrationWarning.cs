namespace ElenasBusinessLedger.Infrastructure.Persistence;

// Carries an upgrade or integrity warning that startup can present to the user.
public sealed record LedgerMigrationWarning(string Title, string Message);
