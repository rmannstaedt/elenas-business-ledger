using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElenasBusinessLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePeriodAndPaymentAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyBankAccount",
                table: "IssuedInvoices",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyMobilePayAccount",
                table: "IssuedInvoices",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodFrom",
                table: "IssuedInvoices",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodTo",
                table: "IssuedInvoices",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "BankAccount",
                table: "CompanyProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MobilePayAccount",
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
                name: "CompanyBankAccount",
                table: "IssuedInvoices");

            migrationBuilder.DropColumn(
                name: "CompanyMobilePayAccount",
                table: "IssuedInvoices");

            migrationBuilder.DropColumn(
                name: "PeriodFrom",
                table: "IssuedInvoices");

            migrationBuilder.DropColumn(
                name: "PeriodTo",
                table: "IssuedInvoices");

            migrationBuilder.DropColumn(
                name: "BankAccount",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "MobilePayAccount",
                table: "CompanyProfiles");
        }
    }
}
