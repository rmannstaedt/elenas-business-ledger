using ElenasBusinessLedger.Core.Company;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Infrastructure.Persistence;

// Stores the settings row for company details, invoice options, and payment instructions.
public sealed class CompanyProfileEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Cvr { get; set; }

    public DateOnly? VatRegistrationDate { get; set; }

    public VatStatus VatStatus { get; set; }

    public string InvoiceFolderPath { get; set; } = string.Empty;

    public string ConfiguredInvoicePrinterName { get; set; } = string.Empty;

    public string PaymentInstructions { get; set; } = string.Empty;

    public string BankRegistrationNumber { get; set; } = string.Empty;

    public string BankAccountNumber { get; set; } = string.Empty;

    public string MobilePayAccount { get; set; } = string.Empty;
}

// Stores the selected calendar and account so the app can reconnect on a later launch.
public sealed class CalendarSettingsEntity
{
    public int Id { get; set; }

    public string SelectedCalendarId { get; set; } = string.Empty;

    public string GoogleAccountEmail { get; set; } = string.Empty;
}

// Persists the first app-backed month that separates legacy reporting from ledger-backed reporting.
public sealed class EconomySettingsEntity
{
    public int Id { get; set; }

    public DateOnly FirstApplicationInvoiceMonth { get; set; }
}

// Stores a month's manually entered net and VAT amounts for legacy reporting.
public sealed class EconomyManualMonthOverrideEntity
{
    public int Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal NetAmount { get; set; }

    public decimal VatAmount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

// Persists reusable teaching terms and their lifecycle state for plan assignment.
public sealed class PlanCatalogEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string PriceWithoutVat { get; set; } = string.Empty;

    public LessonsPerWeek LessonsPerWeek { get; set; }

    public int DurationMinutes { get; set; }

    public PlanFlexibility Flexibility { get; set; }

    public int NumberOfStudents { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }

    public string PaymentTerms { get; set; } = string.Empty;

    public DateTimeOffset? RetireOn { get; set; }

    public PlanCatalogState State { get; set; }
}

// Stores student identity and profile fields with the links to schedules, assigned plans, and lessons.
public sealed class StudentEntity
{
    public int Id { get; set; }

    public string ShortName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public AgeBracket AgeBracket { get; set; }

    public DateOnly? Birthdate { get; set; }

    public bool IsActive { get; set; }

    public List<StudentPlanPeriodEntity> PlanPeriods { get; } = [];

    public List<StudentScheduleEntryEntity> ScheduleEntries { get; } = [];

    public List<LessonEntity> Lessons { get; } = [];
}

// Stores assigned plan terms and dates under the student and source plan IDs.
public sealed class StudentPlanPeriodEntity
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public StudentEntity Student { get; set; } = null!;

    public int PlanCatalogId { get; set; }

    public PlanCatalogEntity PlanCatalog { get; set; } = null!;

    public string PlanName { get; set; } = string.Empty;

    public decimal PriceWithoutVat { get; set; }

    public LessonsPerWeek LessonsPerWeek { get; set; }

    public int DurationMinutes { get; set; }

    public PlanFlexibility Flexibility { get; set; }

    public int NumberOfStudents { get; set; }

    public DateTimeOffset PlanEffectiveFrom { get; set; }

    public string PaymentTerms { get; set; } = string.Empty;

    public DateOnly StartsOn { get; set; }

    public DateOnly? EndsOn { get; set; }
}

// Records when a plan's label changed so the catalog can retain its rename history.
public sealed class PlanNameChangeEntity
{
    public int Id { get; set; }

    public int PlanCatalogId { get; set; }

    public PlanCatalogEntity PlanCatalog { get; set; } = null!;

    public string OldName { get; set; } = string.Empty;

    public string NewName { get; set; } = string.Empty;

    public DateTimeOffset ChangedAt { get; set; }
}

// Stores one student's recurring weekday and start time for monthly calendar population.
public sealed class StudentScheduleEntryEntity
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public StudentEntity Student { get; set; } = null!;

    public DayOfWeek Weekday { get; set; }

    public TimeOnly StartsAt { get; set; }
}

// Persists the numbered billing snapshot, PDF recovery state, and payment and print status.
public sealed class IssuedInvoiceEntity
{
    public int Id { get; set; }

    public int InvoiceNumber { get; set; }

    public DateOnly InvoiceDate { get; set; }

    public DateOnly PeriodFrom { get; set; }

    public DateOnly PeriodTo { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string CompanyAddress { get; set; } = string.Empty;

    public string? CompanyCvr { get; set; }

    public string CompanyPaymentInstructions { get; set; } = string.Empty;

    public string CompanyBankRegistrationNumber { get; set; } = string.Empty;

    public string CompanyBankAccountNumber { get; set; } = string.Empty;

    public string CompanyMobilePayAccount { get; set; } = string.Empty;

    public int? StudentId { get; set; }

    public string StudentShortName { get; set; } = string.Empty;

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentAddress { get; set; } = string.Empty;

    public decimal? PriceAdjustmentAmount { get; set; }

    public string? PriceAdjustmentReason { get; set; }

    public string? SnapshotRevision { get; set; }

    public string? StagedPdfPath { get; set; }

    public string? PlannedPdfPath { get; set; }

    public string? PdfSha256 { get; set; }

    public decimal TotalWithoutVatAmount { get; set; }

    public decimal VatAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "DKK";

    public string PdfFilePath { get; set; } = string.Empty;

    public PaymentStatus PaymentStatus { get; set; }

    public InvoiceIssueStatus IssueStatus { get; set; } = InvoiceIssueStatus.Issued;

    public InvoicePrintStatus PrintStatus { get; set; } = InvoicePrintStatus.NotPrinted;

    public DateTimeOffset? PrintedAt { get; set; }

    public string? LastPrintError { get; set; }

    public List<IssuedInvoiceLineEntity> Lines { get; } = [];
}

// Stores invoiced lesson details and the local lesson link used to enforce billing ownership.
public sealed class IssuedInvoiceLineEntity
{
    public int Id { get; set; }

    public int IssuedInvoiceEntityId { get; set; }

    public IssuedInvoiceEntity Invoice { get; set; } = null!;

    public string LessonDateTime { get; set; } = string.Empty;

    public bool HasKnownLessonTime { get; set; } = true;

    public string PlanName { get; set; } = string.Empty;

    public int PlanCatalogId { get; set; }

    public int LessonId { get; set; }

    public LessonEntity Lesson { get; set; } = null!;

    public decimal PriceWithoutVatAmount { get; set; }

    public string Currency { get; set; } = "DKK";
}

// Persists the local lesson and its student, plan, calendar event, and billing state.
public sealed class LessonEntity
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public StudentEntity Student { get; set; } = null!;

    public int PlanCatalogId { get; set; }

    public PlanCatalogEntity PlanCatalog { get; set; } = null!;

    public string PlanName { get; set; } = string.Empty;

    public DateTimeOffset StartsAt { get; set; }

    public bool HasKnownStartTime { get; set; } = true;

    public int DurationMinutes { get; set; }

    public LessonState State { get; set; }

    public string? GoogleCalendarEventId { get; set; }

    public CalendarMutationKind? PendingCalendarMutation { get; set; }

    public int? InvoiceNumber { get; set; }
}
