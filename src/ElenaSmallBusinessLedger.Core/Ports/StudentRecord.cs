using ElenaSmallBusinessLedger.Core.Plans;
using ElenaSmallBusinessLedger.Core.Students;

namespace ElenaSmallBusinessLedger.Core.Ports;

public sealed record StudentRecord(Student Student, IReadOnlyList<StudentPlanPeriod> PlanPeriods);
