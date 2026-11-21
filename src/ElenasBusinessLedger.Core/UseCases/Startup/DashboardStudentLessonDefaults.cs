using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Startup;

public sealed record DashboardStudentLessonDefaults(
    string StudentShortName,
    IReadOnlyList<StudentScheduleEntry> Schedule,
    IReadOnlyList<StudentPlanPeriod> PlanPeriods);
