using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElenasBusinessLedger.Infrastructure.Persistence.Migrations;

// Adds stable student ownership to invoices and backfills it when the saved lesson links agree.
/// <inheritdoc />
public partial class _20261121165836_LinkInvoiceStudent : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "StudentId",
            table: "IssuedInvoices",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_IssuedInvoices_StudentId",
            table: "IssuedInvoices",
            column: "StudentId");

        migrationBuilder.AddForeignKey(
            name: "FK_IssuedInvoices_Students_StudentId",
            table: "IssuedInvoices",
            column: "StudentId",
            principalTable: "Students",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql(
            """
            UPDATE "IssuedInvoices" AS invoice
            SET "StudentId" = (
                SELECT MIN(lesson."StudentId")
                FROM "IssuedInvoiceLines" line
                JOIN "Lessons" lesson ON lesson."Id" = line."LessonId"
                WHERE line."IssuedInvoiceEntityId" = invoice."Id"
                HAVING COUNT(DISTINCT lesson."StudentId") = 1
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_IssuedInvoices_Students_StudentId",
            table: "IssuedInvoices");

        migrationBuilder.DropIndex(
            name: "IX_IssuedInvoices_StudentId",
            table: "IssuedInvoices");

        migrationBuilder.DropColumn(
            name: "StudentId",
            table: "IssuedInvoices");
    }
}
