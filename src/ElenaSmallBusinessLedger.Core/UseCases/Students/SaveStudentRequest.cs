using ElenaSmallBusinessLedger.Core.Plans;
using ElenaSmallBusinessLedger.Core.Students;

namespace ElenaSmallBusinessLedger.Core.UseCases.Students;

public sealed record StudentPlanPeriodInput(
    string PlanName,
    decimal PriceWithoutVat,
    LessonsPerWeek LessonsPerWeek,
    int DurationMinutes,
    PlanFlexibility Flexibility,
    int NumberOfStudents,
    DateTimeOffset PlanEffectiveFrom,
    DateOnly StartsOn,
    DateOnly? EndsOn);

public sealed record StudentScheduleInput(DayOfWeek Weekday, TimeOnly StartsAt);

public sealed record SaveStudentRequest(
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
