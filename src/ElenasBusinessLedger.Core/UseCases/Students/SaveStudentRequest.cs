using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Students;

// Carries an editable student plan snapshot and its validity dates to the save workflow.
public sealed record StudentPlanPeriodInput(
    string PlanName,
    decimal PriceWithoutVat,
    LessonsPerWeek LessonsPerWeek,
    int DurationMinutes,
    PlanFlexibility Flexibility,
    int NumberOfStudents,
    DateTimeOffset PlanEffectiveFrom,
    DateOnly StartsOn,
    DateOnly? EndsOn,
    string PaymentTerms = "",
    int PlanCatalogId = 0);

// Carries one editable recurring time so the student save can rebuild the stored schedule.
public sealed record StudentScheduleInput(DayOfWeek Weekday, TimeOnly StartsAt);

// Carries the student's ID, edited profile, plans, and schedule for an explicit save.
public sealed record SaveStudentRequest(
    int StudentId,
    string ShortName,
    string FullName,
    string Address,
    string Phone,
    string Email,
    AgeBracket AgeBracket,
    DateOnly? Birthdate,
    bool IsActive,
    IReadOnlyList<StudentPlanPeriodInput> PlanPeriods,
    IReadOnlyList<StudentScheduleInput> Schedule);
