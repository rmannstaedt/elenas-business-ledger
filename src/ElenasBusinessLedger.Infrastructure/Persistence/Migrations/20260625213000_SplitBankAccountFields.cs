using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElenasBusinessLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(LedgerDbContext))]
    [Migration("20260625213000_SplitBankAccountFields")]
    public partial class SplitBankAccountFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BankAccount",
                table: "CompanyProfiles",
                newName: "BankAccountNumber");

            migrationBuilder.AddColumn<string>(
                name: "BankRegistrationNumber",
                table: "CompanyProfiles",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.RenameColumn(
                name: "CompanyBankAccount",
                table: "IssuedInvoices",
                newName: "CompanyBankAccountNumber");

            migrationBuilder.AddColumn<string>(
                name: "CompanyBankRegistrationNumber",
                table: "IssuedInvoices",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankRegistrationNumber",
                table: "CompanyProfiles");

            migrationBuilder.RenameColumn(
                name: "BankAccountNumber",
                table: "CompanyProfiles",
                newName: "BankAccount");

            migrationBuilder.DropColumn(
                name: "CompanyBankRegistrationNumber",
                table: "IssuedInvoices");

            migrationBuilder.RenameColumn(
                name: "CompanyBankAccountNumber",
                table: "IssuedInvoices",
                newName: "CompanyBankAccount");
        }
    }
}
