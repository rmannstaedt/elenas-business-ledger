using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySmallBusinessLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanCatalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    GoogleCalendarEventId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    InvoiceNumber = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lessons_PlanCatalog_PlanCatalogId",
                        column: x => x.PlanCatalogId,
                        principalTable: "PlanCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lessons_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_GoogleCalendarEventId",
                table: "Lessons",
                column: "GoogleCalendarEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_PlanCatalogId",
                table: "Lessons",
                column: "PlanCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_StartsAt",
                table: "Lessons",
                column: "StartsAt");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_StudentId_StartsAt",
                table: "Lessons",
                columns: new[] { "StudentId", "StartsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lessons");
        }
    }
}
