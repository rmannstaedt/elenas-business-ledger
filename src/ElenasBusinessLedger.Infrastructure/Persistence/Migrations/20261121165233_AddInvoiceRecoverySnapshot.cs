using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElenasBusinessLedger.Infrastructure.Persistence.Migrations;

// Adds revision, adjustment, and PDF checkpoint fields so interrupted issuance can resume from saved details.
/// <inheritdoc />
public partial class _20261121165233_AddInvoiceRecoverySnapshot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PdfSha256",
            table: "IssuedInvoices",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PlannedPdfPath",
            table: "IssuedInvoices",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "PriceAdjustmentAmount",
            table: "IssuedInvoices",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PriceAdjustmentReason",
            table: "IssuedInvoices",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SnapshotRevision",
            table: "IssuedInvoices",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StagedPdfPath",
            table: "IssuedInvoices",
            type: "TEXT",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PdfSha256",
            table: "IssuedInvoices");

        migrationBuilder.DropColumn(
            name: "PlannedPdfPath",
            table: "IssuedInvoices");

        migrationBuilder.DropColumn(
            name: "PriceAdjustmentAmount",
            table: "IssuedInvoices");

        migrationBuilder.DropColumn(
            name: "PriceAdjustmentReason",
            table: "IssuedInvoices");

        migrationBuilder.DropColumn(
            name: "SnapshotRevision",
            table: "IssuedInvoices");

        migrationBuilder.DropColumn(
            name: "StagedPdfPath",
            table: "IssuedInvoices");
    }
}
