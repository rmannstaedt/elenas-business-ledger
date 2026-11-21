namespace ElenasBusinessLedger.Core.UseCases.Startup;

// Pairs an assignable plan's ID with its label so the dashboard can update the correct lesson plan.
public sealed record DashboardPlanOption(int PlanCatalogId, string Name);
