using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Students;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

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
}

public sealed class CalendarSettingsEntity
{
    public int Id { get; set; }

    public string SelectedCalendarId { get; set; } = string.Empty;

    public string GoogleAccountEmail { get; set; } = string.Empty;
}

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
}

public sealed class StudentPlanPeriodEntity
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public StudentEntity Student { get; set; } = null!;

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

public sealed class StudentScheduleEntryEntity
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public StudentEntity Student { get; set; } = null!;

    public DayOfWeek Weekday { get; set; }

    public TimeOnly StartsAt { get; set; }
}

public sealed class IssuedInvoiceEntity
{
    public int Id { get; set; }

    public int InvoiceNumber { get; set; }

    public DateOnly InvoiceDate { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string CompanyAddress { get; set; } = string.Empty;

    public string? CompanyCvr { get; set; }

    public string StudentShortName { get; set; } = string.Empty;

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentAddress { get; set; } = string.Empty;

    public decimal TotalWithoutVatAmount { get; set; }

    public decimal VatAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "DKK";

    public string PdfFilePath { get; set; } = string.Empty;

    public PaymentStatus PaymentStatus { get; set; }

    public List<IssuedInvoiceLineEntity> Lines { get; } = [];
}

public sealed class IssuedInvoiceLineEntity
{
    public int Id { get; set; }

    public int IssuedInvoiceEntityId { get; set; }

    public IssuedInvoiceEntity Invoice { get; set; } = null!;

    public string LessonDateTime { get; set; } = string.Empty;

    public string PlanName { get; set; } = string.Empty;

    public decimal PriceWithoutVatAmount { get; set; }

    public string Currency { get; set; } = "DKK";
}
