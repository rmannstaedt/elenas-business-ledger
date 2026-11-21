using ElenasBusinessLedger.Core.Plans;

namespace ElenasBusinessLedger.App.ViewModels;

// Formats a catalog plan's terms and lifecycle state for selection and display in the plan list.
public sealed class PlanCatalogDisplayModel
{
    public PlanCatalogDisplayModel(PlanCatalogItem plan)
    {
        Plan = plan;
    }

    public PlanCatalogItem Plan { get; }

    public int Id => Plan.Id;

    public string Name => Plan.Name;

    public PlanCatalogState State => Plan.State;

    public string StateText => Plan.State switch
    {
        PlanCatalogState.Draft => "Draft",
        PlanCatalogState.Active => "Active",
        PlanCatalogState.Retired => "Retired",
        _ => "Unknown"
    };

    public string PriceWithoutVatText => Plan.PriceWithoutVat?.ToString("0.##") ?? string.Empty;

    public string DurationMinutesText => Plan.DurationMinutes.ToString();

    public string LessonsPerWeekText => Plan.LessonsPerWeek switch
    {
        LessonsPerWeek.One => "1 / week",
        LessonsPerWeek.Two => "2 / week",
        LessonsPerWeek.ByAppointment => "By appointment",
        _ => Plan.LessonsPerWeek.ToString()
    };

    public string NumberOfStudentsText => Plan.NumberOfStudents.ToString();
}
