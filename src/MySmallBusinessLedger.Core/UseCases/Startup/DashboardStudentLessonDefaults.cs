using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Students;

namespace MySmallBusinessLedger.Core.UseCases.Startup;

public sealed record DashboardStudentLessonDefaults(
    string StudentShortName,
    IReadOnlyList<StudentScheduleEntry> Schedule,
    IReadOnlyList<StudentPlanPeriod> PlanPeriods);
