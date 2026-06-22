using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySmallBusinessLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "CalendarSettings" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_CalendarSettings" PRIMARY KEY AUTOINCREMENT,
                    "SelectedCalendarId" TEXT NOT NULL,
                    "GoogleAccountEmail" TEXT NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "CompanyProfiles" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_CompanyProfiles" PRIMARY KEY AUTOINCREMENT,
                    "Name" TEXT NOT NULL,
                    "Address" TEXT NOT NULL,
                    "Phone" TEXT NOT NULL,
                    "Email" TEXT NOT NULL,
                    "Cvr" TEXT NULL,
                    "VatRegistrationDate" TEXT NULL,
                    "VatStatus" TEXT NOT NULL,
                    "InvoiceFolderPath" TEXT NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "IssuedInvoices" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_IssuedInvoices" PRIMARY KEY AUTOINCREMENT,
                    "InvoiceNumber" INTEGER NOT NULL,
                    "InvoiceDate" TEXT NOT NULL,
                    "CompanyName" TEXT NOT NULL,
                    "CompanyAddress" TEXT NOT NULL,
                    "CompanyCvr" TEXT NULL,
                    "StudentShortName" TEXT NOT NULL,
                    "StudentFullName" TEXT NOT NULL,
                    "StudentAddress" TEXT NOT NULL,
                    "TotalWithoutVatAmount" TEXT NOT NULL,
                    "VatAmount" TEXT NOT NULL,
                    "TotalAmount" TEXT NOT NULL,
                    "Currency" TEXT NOT NULL,
                    "PdfFilePath" TEXT NOT NULL,
                    "PaymentStatus" TEXT NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "PlanCatalog" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_PlanCatalog" PRIMARY KEY AUTOINCREMENT,
                    "Name" TEXT NOT NULL,
                    "PriceWithoutVat" TEXT NOT NULL,
                    "LessonsPerWeek" TEXT NOT NULL,
                    "DurationMinutes" INTEGER NOT NULL,
                    "Flexibility" TEXT NOT NULL,
                    "NumberOfStudents" INTEGER NOT NULL,
                    "EffectiveFrom" TEXT NOT NULL,
                    "PaymentTerms" TEXT NOT NULL,
                    "RetireOn" TEXT NULL,
                    "State" TEXT NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Students" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Students" PRIMARY KEY AUTOINCREMENT,
                    "ShortName" TEXT NOT NULL,
                    "FullName" TEXT NOT NULL,
                    "Address" TEXT NOT NULL,
                    "Phone" TEXT NOT NULL,
                    "Email" TEXT NOT NULL,
                    "AgeBracket" TEXT NOT NULL,
                    "Birthdate" TEXT NULL,
                    "IsActive" INTEGER NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "IssuedInvoiceLines" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_IssuedInvoiceLines" PRIMARY KEY AUTOINCREMENT,
                    "IssuedInvoiceEntityId" INTEGER NOT NULL,
                    "LessonDateTime" TEXT NOT NULL,
                    "PlanName" TEXT NOT NULL,
                    "PlanCatalogId" INTEGER NOT NULL,
                    "PriceWithoutVatAmount" TEXT NOT NULL,
                    "Currency" TEXT NOT NULL,
                    CONSTRAINT "FK_IssuedInvoiceLines_IssuedInvoices_IssuedInvoiceEntityId"
                        FOREIGN KEY ("IssuedInvoiceEntityId")
                        REFERENCES "IssuedInvoices" ("Id")
                        ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "PlanNameChanges" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_PlanNameChanges" PRIMARY KEY AUTOINCREMENT,
                    "PlanCatalogId" INTEGER NOT NULL,
                    "OldName" TEXT NOT NULL,
                    "NewName" TEXT NOT NULL,
                    "ChangedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_PlanNameChanges_PlanCatalog_PlanCatalogId"
                        FOREIGN KEY ("PlanCatalogId")
                        REFERENCES "PlanCatalog" ("Id")
                        ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "StudentPlanPeriods" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_StudentPlanPeriods" PRIMARY KEY AUTOINCREMENT,
                    "StudentId" INTEGER NOT NULL,
                    "PlanCatalogId" INTEGER NOT NULL,
                    "PlanName" TEXT NOT NULL,
                    "PriceWithoutVat" TEXT NOT NULL,
                    "LessonsPerWeek" TEXT NOT NULL,
                    "DurationMinutes" INTEGER NOT NULL,
                    "Flexibility" TEXT NOT NULL,
                    "NumberOfStudents" INTEGER NOT NULL,
                    "PlanEffectiveFrom" TEXT NOT NULL,
                    "PaymentTerms" TEXT NOT NULL,
                    "StartsOn" TEXT NOT NULL,
                    "EndsOn" TEXT NULL,
                    CONSTRAINT "FK_StudentPlanPeriods_PlanCatalog_PlanCatalogId"
                        FOREIGN KEY ("PlanCatalogId")
                        REFERENCES "PlanCatalog" ("Id")
                        ON DELETE RESTRICT,
                    CONSTRAINT "FK_StudentPlanPeriods_Students_StudentId"
                        FOREIGN KEY ("StudentId")
                        REFERENCES "Students" ("Id")
                        ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "StudentScheduleEntries" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_StudentScheduleEntries" PRIMARY KEY AUTOINCREMENT,
                    "StudentId" INTEGER NOT NULL,
                    "Weekday" TEXT NOT NULL,
                    "StartsAt" TEXT NOT NULL,
                    CONSTRAINT "FK_StudentScheduleEntries_Students_StudentId"
                        FOREIGN KEY ("StudentId")
                        REFERENCES "Students" ("Id")
                        ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_IssuedInvoiceLines_IssuedInvoiceEntityId" ON "IssuedInvoiceLines" ("IssuedInvoiceEntityId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_IssuedInvoiceLines_LessonDateTime" ON "IssuedInvoiceLines" ("LessonDateTime");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_IssuedInvoiceLines_PlanCatalogId" ON "IssuedInvoiceLines" ("PlanCatalogId");""");
            migrationBuilder.Sql("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_IssuedInvoices_InvoiceNumber" ON "IssuedInvoices" ("InvoiceNumber");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_PlanCatalog_Name" ON "PlanCatalog" ("Name");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_PlanNameChanges_PlanCatalogId" ON "PlanNameChanges" ("PlanCatalogId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_StudentPlanPeriods_PlanCatalogId" ON "StudentPlanPeriods" ("PlanCatalogId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_StudentPlanPeriods_StudentId" ON "StudentPlanPeriods" ("StudentId");""");
            migrationBuilder.Sql("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_Students_ShortName" ON "Students" ("ShortName");""");
            migrationBuilder.Sql("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_StudentScheduleEntries_StudentId_Weekday_StartsAt" ON "StudentScheduleEntries" ("StudentId", "Weekday", "StartsAt");""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "CalendarSettings";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "CompanyProfiles";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "IssuedInvoiceLines";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "PlanNameChanges";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "StudentPlanPeriods";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "StudentScheduleEntries";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "IssuedInvoices";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "PlanCatalog";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "Students";""");
        }
    }
}
