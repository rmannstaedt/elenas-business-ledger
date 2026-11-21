using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Students;

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

public sealed record StudentScheduleInput(DayOfWeek Weekday, TimeOnly StartsAt);

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
