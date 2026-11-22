using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ElenasBusinessLedger.Infrastructure.Persistence.Migrations;

// Prevents a second pending reservation in SQLite so concurrent issuers cannot advance the number sequence.
[DbContext(typeof(LedgerDbContext))]
[Migration("20261121190000_GuardInvoiceSequence")]
public sealed class GuardInvoiceSequence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TRIGGER "InvoiceSequence_OnePendingInsert"
            BEFORE INSERT ON "IssuedInvoices"
            WHEN NEW."IssueStatus" = 'Issuing'
                AND EXISTS (SELECT 1 FROM "IssuedInvoices" WHERE "IssueStatus" = 'Issuing')
            BEGIN
                SELECT RAISE(ABORT, 'Resolve the pending invoice before reserving a new number.');
            END;

            CREATE TRIGGER "InvoiceSequence_OnePendingUpdate"
            BEFORE UPDATE OF "IssueStatus" ON "IssuedInvoices"
            WHEN NEW."IssueStatus" = 'Issuing' AND OLD."IssueStatus" <> 'Issuing'
                AND EXISTS (SELECT 1 FROM "IssuedInvoices" WHERE "IssueStatus" = 'Issuing')
            BEGIN
                SELECT RAISE(ABORT, 'Resolve the pending invoice before reserving a new number.');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER "InvoiceSequence_OnePendingUpdate";
            DROP TRIGGER "InvoiceSequence_OnePendingInsert";
            """);
    }
}
