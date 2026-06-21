using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Students;

namespace MySmallBusinessLedger.Core.Ports;

public sealed record StudentRecord(
    Student Student,
    IReadOnlyList<StudentPlanPeriod> PlanPeriods,
    IReadOnlyList<StudentScheduleEntry> Schedule);
