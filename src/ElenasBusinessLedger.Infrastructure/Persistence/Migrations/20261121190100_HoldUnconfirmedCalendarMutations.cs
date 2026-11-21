using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ElenasBusinessLedger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LedgerDbContext))]
[Migration("20261121190100_HoldUnconfirmedCalendarMutations")]
public sealed class HoldUnconfirmedCalendarMutations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(name: "PendingCalendarMutation", table: "Lessons", type: "TEXT", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""ALTER TABLE "Lessons" DROP COLUMN "PendingCalendarMutation";""");
}
