using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Core.UseCases.Startup;

// Supplies the selected student's schedule and assigned periods so the dashboard can prefill a new lesson.
public sealed record DashboardStudentLessonDefaults(
    string StudentShortName,
    IReadOnlyList<StudentScheduleEntry> Schedule,
    IReadOnlyList<StudentPlanPeriod> PlanPeriods);
