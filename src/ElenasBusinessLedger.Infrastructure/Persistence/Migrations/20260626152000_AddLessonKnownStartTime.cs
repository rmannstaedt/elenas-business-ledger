using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElenasBusinessLedger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LedgerDbContext))]
[Migration("20260626152000_AddLessonKnownStartTime")]
public partial class AddLessonKnownStartTime : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "HasKnownStartTime",
            table: "Lessons",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "HasKnownLessonTime",
            table: "IssuedInvoiceLines",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HasKnownStartTime",
            table: "Lessons");

        migrationBuilder.DropColumn(
            name: "HasKnownLessonTime",
            table: "IssuedInvoiceLines");
    }
}
