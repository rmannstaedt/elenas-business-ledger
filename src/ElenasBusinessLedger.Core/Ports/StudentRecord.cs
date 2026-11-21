using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.Ports;

public sealed record StudentRecord(
    int Id,
    Student Student,
    IReadOnlyList<StudentPlanPeriod> PlanPeriods,
    IReadOnlyList<StudentScheduleEntry> Schedule);
