using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ElenasBusinessLedger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LedgerDbContext))]
[Migration("20260627165900_ArchiveUnlinkedInvoiceLines")]
public sealed class ArchiveUnlinkedInvoiceLines : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Runs before the legacy line rebuild for older databases and is harmless on databases that already rebuilt.
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "ArchivedInvoiceLines" (
                "OriginalLineId" INTEGER NOT NULL PRIMARY KEY,
                "InvoiceNumber" INTEGER NOT NULL,
                "LessonDateTime" TEXT NOT NULL,
                "PlanName" TEXT NOT NULL,
                "PlanCatalogId" INTEGER NOT NULL,
                "PriceWithoutVatAmount" TEXT NOT NULL,
                "Currency" TEXT NOT NULL,
                "HasKnownLessonTime" INTEGER NOT NULL,
                "OriginalLessonId" INTEGER NOT NULL
            );

            INSERT INTO "ArchivedInvoiceLines" (
                "OriginalLineId", "InvoiceNumber", "LessonDateTime", "PlanName", "PlanCatalogId",
                "PriceWithoutVatAmount", "Currency", "HasKnownLessonTime", "OriginalLessonId")
            SELECT line."Id", invoice."InvoiceNumber", line."LessonDateTime", line."PlanName", line."PlanCatalogId",
                line."PriceWithoutVatAmount", line."Currency", line."HasKnownLessonTime", line."LessonId"
            FROM "IssuedInvoiceLines" line
            JOIN "IssuedInvoices" invoice ON invoice."Id" = line."IssuedInvoiceEntityId"
            WHERE (
                line."LessonId" <= 0
                OR NOT EXISTS (SELECT 1 FROM "Lessons" lesson WHERE lesson."Id" = line."LessonId")
                OR line."Id" <> (SELECT MIN(duplicate."Id") FROM "IssuedInvoiceLines" duplicate WHERE duplicate."LessonId" = line."LessonId")
            )
            AND NOT EXISTS (SELECT 1 FROM "ArchivedInvoiceLines" archived WHERE archived."OriginalLineId" = line."Id");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve the archive when returning to an older schema.
    }
}
