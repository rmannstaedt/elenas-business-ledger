using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElenasBusinessLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentInstructions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyPaymentInstructions",
                table: "IssuedInvoices",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentInstructions",
                table: "CompanyProfiles",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyPaymentInstructions",
                table: "IssuedInvoices");

            migrationBuilder.DropColumn(
                name: "PaymentInstructions",
                table: "CompanyProfiles");
        }
    }
}
