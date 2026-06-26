using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySmallBusinessLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceIssuePrintState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IssueStatus",
                table: "IssuedInvoices",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "Issued");

            migrationBuilder.AddColumn<string>(
                name: "LastPrintError",
                table: "IssuedInvoices",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrintStatus",
                table: "IssuedInvoices",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "NotPrinted");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PrintedAt",
                table: "IssuedInvoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfiguredInvoicePrinterName",
                table: "CompanyProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssueStatus",
                table: "IssuedInvoices");

            migrationBuilder.DropColumn(
                name: "LastPrintError",
                table: "IssuedInvoices");

            migrationBuilder.DropColumn(
                name: "PrintStatus",
                table: "IssuedInvoices");

            migrationBuilder.DropColumn(
                name: "PrintedAt",
                table: "IssuedInvoices");

            migrationBuilder.DropColumn(
                name: "ConfiguredInvoicePrinterName",
                table: "CompanyProfiles");
        }
    }
}
