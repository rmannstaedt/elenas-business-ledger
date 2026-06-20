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
                "PaymentTerms" TEXT NOT NULL DEFAULT '',
                "RetireOn" TEXT NULL,
                "State" TEXT NOT NULL
            );
            """);

        EnsureColumn(context, "PlanCatalog", "RetireOn", "TEXT NULL");
        EnsureColumn(context, "PlanCatalog", "PaymentTerms", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(context, "StudentPlanPeriods", "PaymentTerms", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(context, "StudentPlanPeriods", "PlanCatalogId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(context, "IssuedInvoiceLines", "PlanCatalogId", "INTEGER NOT NULL DEFAULT 0");
        EnsurePlanNameChangesTable(context);

        context.Database.ExecuteSqlRaw("""
            UPDATE "PlanCatalog"
            SET "State" = 'Retired'
            WHERE "State" = 'Archived';
            """);

        context.Database.ExecuteSqlRaw("""
            UPDATE "PlanCatalog"
            SET "PaymentTerms" = 'Paid monthly in advance'
            WHERE "PaymentTerms" = '';
            """);

        context.Database.ExecuteSqlRaw("""
            INSERT INTO "PlanCatalog" (
                "Name",
                "PriceWithoutVat",
                "LessonsPerWeek",
                "DurationMinutes",
                "Flexibility",
                "NumberOfStudents",
                "EffectiveFrom",
                "PaymentTerms",
                "RetireOn",
                "State"
            )
            SELECT
                "StudentPlanPeriods"."PlanName",
                CAST("StudentPlanPeriods"."PriceWithoutVat" AS TEXT),
                "StudentPlanPeriods"."LessonsPerWeek",
                "StudentPlanPeriods"."DurationMinutes",
                "StudentPlanPeriods"."Flexibility",
                "StudentPlanPeriods"."NumberOfStudents",
                "StudentPlanPeriods"."PlanEffectiveFrom",
                "StudentPlanPeriods"."PaymentTerms",
                NULL,
                'Active'
            FROM "StudentPlanPeriods"
            WHERE "StudentPlanPeriods"."PlanCatalogId" = 0
              AND "StudentPlanPeriods"."PlanName" <> ''
              AND NOT EXISTS (
                  SELECT 1
                  FROM "PlanCatalog"
                  WHERE "PlanCatalog"."Name" = "StudentPlanPeriods"."PlanName"
              )
            GROUP BY "StudentPlanPeriods"."PlanName";
            """);

        context.Database.ExecuteSqlRaw("""
            UPDATE "StudentPlanPeriods"
            SET "PlanCatalogId" = (
                SELECT "PlanCatalog"."Id"
                FROM "PlanCatalog"
                WHERE "PlanCatalog"."Name" = "StudentPlanPeriods"."PlanName"
                LIMIT 1
            )
            WHERE "PlanCatalogId" = 0
              AND EXISTS (
                  SELECT 1
                  FROM "PlanCatalog"
                  WHERE "PlanCatalog"."Name" = "StudentPlanPeriods"."PlanName"
              );
            """);

        context.Database.ExecuteSqlRaw("""
            UPDATE "StudentPlanPeriods"
            SET "PaymentTerms" = 'Paid monthly in advance'
            WHERE "PaymentTerms" = '';
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_PlanCatalog_Name"
            ON "PlanCatalog" ("Name");
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_StudentPlanPeriods_PlanCatalogId"
            ON "StudentPlanPeriods" ("PlanCatalogId");
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_IssuedInvoiceLines_PlanCatalogId"
            ON "IssuedInvoiceLines" ("PlanCatalogId");
            """);
    }

    private static void EnsureColumn(
        LedgerDbContext context,
        string tableName,
        string columnName,
        string columnDefinition)
    {
        var existingColumnsSql = tableName switch
        {
            "PlanCatalog" => """SELECT "name" AS "Value" FROM pragma_table_info('PlanCatalog')""",
            "StudentPlanPeriods" => """SELECT "name" AS "Value" FROM pragma_table_info('StudentPlanPeriods')""",
            "IssuedInvoiceLines" => """SELECT "name" AS "Value" FROM pragma_table_info('IssuedInvoiceLines')""",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "Unknown schema table.")
        };
        var existingColumns = context.Database
            .SqlQueryRaw<string>(existingColumnsSql)
            .ToArray();
        if (!existingColumns.Contains(columnName, StringComparer.Ordinal))
        {
            var addColumnSql = (tableName, columnName, columnDefinition) switch
            {
                ("PlanCatalog", "RetireOn", "TEXT NULL") =>
                    """ALTER TABLE "PlanCatalog" ADD COLUMN "RetireOn" TEXT NULL;""",
                ("PlanCatalog", "PaymentTerms", "TEXT NOT NULL DEFAULT ''") =>
                    """ALTER TABLE "PlanCatalog" ADD COLUMN "PaymentTerms" TEXT NOT NULL DEFAULT '';""",
                ("StudentPlanPeriods", "PaymentTerms", "TEXT NOT NULL DEFAULT ''") =>
                    """ALTER TABLE "StudentPlanPeriods" ADD COLUMN "PaymentTerms" TEXT NOT NULL DEFAULT '';""",
                ("StudentPlanPeriods", "PlanCatalogId", "INTEGER NOT NULL DEFAULT 0") =>
                    """ALTER TABLE "StudentPlanPeriods" ADD COLUMN "PlanCatalogId" INTEGER NOT NULL DEFAULT 0;""",
                ("IssuedInvoiceLines", "PlanCatalogId", "INTEGER NOT NULL DEFAULT 0") =>
                    """ALTER TABLE "IssuedInvoiceLines" ADD COLUMN "PlanCatalogId" INTEGER NOT NULL DEFAULT 0;""",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(columnName),
                    columnName,
                    "Unknown schema column.")
            };
            context.Database.ExecuteSqlRaw(addColumnSql);
        }
    }

    private static void EnsurePlanNameChangesTable(LedgerDbContext context)
    {
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "PlanNameChanges" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PlanNameChanges" PRIMARY KEY AUTOINCREMENT,
                "PlanCatalogId" INTEGER NOT NULL,
                "OldName" TEXT NOT NULL,
                "NewName" TEXT NOT NULL,
                "ChangedAt" TEXT NOT NULL,
                CONSTRAINT "FK_PlanNameChanges_PlanCatalog_PlanCatalogId"
                    FOREIGN KEY ("PlanCatalogId")
                    REFERENCES "PlanCatalog" ("Id")
                    ON DELETE CASCADE
            );
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_PlanNameChanges_PlanCatalogId"
            ON "PlanNameChanges" ("PlanCatalogId");
            """);
    }
}
