using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElenasBusinessLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuedInvoiceLineLessonId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LessonId",
                table: "IssuedInvoiceLines",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_IssuedInvoiceLines_LessonId",
                table: "IssuedInvoiceLines",
                column: "LessonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IssuedInvoiceLines_LessonId",
                table: "IssuedInvoiceLines");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "IssuedInvoiceLines");
        }
    }
}
