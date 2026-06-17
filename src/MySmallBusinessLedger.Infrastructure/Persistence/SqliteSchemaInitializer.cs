using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

public sealed class SqliteSchemaInitializer
{
    private readonly Func<LedgerDbContext> createContext;

    public SqliteSchemaInitializer(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    public void EnsureCreated()
    {
        using var context = createContext();
        context.Database.EnsureCreated();
        EnsurePlanCatalogTable(context);
    }

    private static void EnsurePlanCatalogTable(LedgerDbContext context)
    {
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "PlanCatalog" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PlanCatalog" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "PriceWithoutVat" TEXT NOT NULL,
                "LessonsPerWeek" TEXT NOT NULL,
                "DurationMinutes" INTEGER NOT NULL,
                "Flexibility" TEXT NOT NULL,
                "NumberOfStudents" INTEGER NOT NULL,
                "EffectiveFrom" TEXT NOT NULL,
                "State" TEXT NOT NULL
            );
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_PlanCatalog_Name"
            ON "PlanCatalog" ("Name");
            """);
    }
}
