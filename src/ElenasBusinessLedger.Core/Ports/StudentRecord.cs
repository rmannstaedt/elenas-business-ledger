using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.Ports;

// Combines the saved student ID with their profile, plan history, and recurring schedule.
public sealed record StudentRecord(
    int Id,
    Student Student,
    IReadOnlyList<StudentPlanPeriod> PlanPeriods,
    IReadOnlyList<StudentScheduleEntry> Schedule);
