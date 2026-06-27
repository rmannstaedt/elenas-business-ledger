using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySmallBusinessLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(LedgerDbContext))]
    [Migration("20260627170000_AddIssuedInvoiceLineLessonReference")]
    public partial class AddIssuedInvoiceLineLessonReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE "IssuedInvoiceLines_new" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_IssuedInvoiceLines" PRIMARY KEY AUTOINCREMENT,
                    "IssuedInvoiceEntityId" INTEGER NOT NULL,
                    "LessonDateTime" TEXT NOT NULL,
                    "PlanName" TEXT NOT NULL,
                    "PlanCatalogId" INTEGER NOT NULL,
                    "PriceWithoutVatAmount" TEXT NOT NULL,
                    "Currency" TEXT NOT NULL,
                    "HasKnownLessonTime" INTEGER NOT NULL DEFAULT 1,
                    "LessonId" INTEGER NOT NULL,
                    CONSTRAINT "FK_IssuedInvoiceLines_IssuedInvoices_IssuedInvoiceEntityId"
                        FOREIGN KEY ("IssuedInvoiceEntityId")
                        REFERENCES "IssuedInvoices" ("Id")
                        ON DELETE CASCADE,
                    CONSTRAINT "FK_IssuedInvoiceLines_Lessons_LessonId"
                        FOREIGN KEY ("LessonId")
                        REFERENCES "Lessons" ("Id")
                        ON DELETE RESTRICT
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "IssuedInvoiceLines_new" (
                    "Id",
                    "IssuedInvoiceEntityId",
                    "LessonDateTime",
                    "PlanName",
                    "PlanCatalogId",
                    "PriceWithoutVatAmount",
                    "Currency",
                    "HasKnownLessonTime",
                    "LessonId")
                SELECT
                    line."Id",
                    line."IssuedInvoiceEntityId",
                    line."LessonDateTime",
                    line."PlanName",
                    line."PlanCatalogId",
                    line."PriceWithoutVatAmount",
                    line."Currency",
                    line."HasKnownLessonTime",
                    line."LessonId"
                FROM "IssuedInvoiceLines" line
                WHERE line."LessonId" > 0
                  AND EXISTS (
                      SELECT 1
                      FROM "Lessons" lesson
                      WHERE lesson."Id" = line."LessonId")
                  AND line."Id" = (
                      SELECT MIN(duplicate."Id")
                      FROM "IssuedInvoiceLines" duplicate
                      WHERE duplicate."LessonId" = line."LessonId");
                """);

            migrationBuilder.Sql("""DROP TABLE "IssuedInvoiceLines";""");
            migrationBuilder.Sql("""ALTER TABLE "IssuedInvoiceLines_new" RENAME TO "IssuedInvoiceLines";""");
            migrationBuilder.Sql("""CREATE INDEX "IX_IssuedInvoiceLines_IssuedInvoiceEntityId" ON "IssuedInvoiceLines" ("IssuedInvoiceEntityId");""");
            migrationBuilder.Sql("""CREATE INDEX "IX_IssuedInvoiceLines_LessonDateTime" ON "IssuedInvoiceLines" ("LessonDateTime");""");
            migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_IssuedInvoiceLines_LessonId" ON "IssuedInvoiceLines" ("LessonId");""");
            migrationBuilder.Sql("""CREATE INDEX "IX_IssuedInvoiceLines_PlanCatalogId" ON "IssuedInvoiceLines" ("PlanCatalogId");""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE "IssuedInvoiceLines_old" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_IssuedInvoiceLines" PRIMARY KEY AUTOINCREMENT,
                    "IssuedInvoiceEntityId" INTEGER NOT NULL,
                    "LessonDateTime" TEXT NOT NULL,
                    "PlanName" TEXT NOT NULL,
                    "PlanCatalogId" INTEGER NOT NULL,
                    "PriceWithoutVatAmount" TEXT NOT NULL,
                    "Currency" TEXT NOT NULL,
                    "HasKnownLessonTime" INTEGER NOT NULL DEFAULT 1,
                    "LessonId" INTEGER NOT NULL,
                    CONSTRAINT "FK_IssuedInvoiceLines_IssuedInvoices_IssuedInvoiceEntityId"
                        FOREIGN KEY ("IssuedInvoiceEntityId")
                        REFERENCES "IssuedInvoices" ("Id")
                        ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "IssuedInvoiceLines_old" (
                    "Id",
                    "IssuedInvoiceEntityId",
                    "LessonDateTime",
                    "PlanName",
                    "PlanCatalogId",
                    "PriceWithoutVatAmount",
                    "Currency",
                    "HasKnownLessonTime",
                    "LessonId")
                SELECT
                    "Id",
                    "IssuedInvoiceEntityId",
                    "LessonDateTime",
                    "PlanName",
                    "PlanCatalogId",
                    "PriceWithoutVatAmount",
                    "Currency",
                    "HasKnownLessonTime",
                    "LessonId"
                FROM "IssuedInvoiceLines";
                """);

            migrationBuilder.Sql("""DROP TABLE "IssuedInvoiceLines";""");
            migrationBuilder.Sql("""ALTER TABLE "IssuedInvoiceLines_old" RENAME TO "IssuedInvoiceLines";""");
            migrationBuilder.Sql("""CREATE INDEX "IX_IssuedInvoiceLines_IssuedInvoiceEntityId" ON "IssuedInvoiceLines" ("IssuedInvoiceEntityId");""");
            migrationBuilder.Sql("""CREATE INDEX "IX_IssuedInvoiceLines_LessonDateTime" ON "IssuedInvoiceLines" ("LessonDateTime");""");
            migrationBuilder.Sql("""CREATE INDEX "IX_IssuedInvoiceLines_LessonId" ON "IssuedInvoiceLines" ("LessonId");""");
            migrationBuilder.Sql("""CREATE INDEX "IX_IssuedInvoiceLines_PlanCatalogId" ON "IssuedInvoiceLines" ("PlanCatalogId");""");
        }
    }
}
