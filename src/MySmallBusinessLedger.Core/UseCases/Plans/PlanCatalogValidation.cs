using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;

namespace MySmallBusinessLedger.Core.UseCases.Plans;

internal static class PlanCatalogValidation
{
    public static IReadOnlyList<ValidationError> ValidateDraft(PlanDraftInput input)
    {
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            errors.Add(new ValidationError("Plan.NameRequired", "Plan name is required."));
        }

        if (input.PriceWithoutVat < 0m)
        {
            errors.Add(new ValidationError("Plan.PriceInvalid", "Plan price cannot be negative."));
        }

        if (!Enum.IsDefined(input.LessonsPerWeek))
        {
            errors.Add(new ValidationError("Plan.LessonsPerWeekInvalid", "Lessons per week is invalid."));
        }

        if (input.DurationMinutes <= 0)
        {
            errors.Add(new ValidationError("Plan.DurationInvalid", "Plan duration must be greater than zero."));
        }

        if (!Enum.IsDefined(input.Flexibility))
        {
            errors.Add(new ValidationError("Plan.FlexibilityInvalid", "Plan flexibility is invalid."));
        }

        if (input.NumberOfStudents <= 0)
        {
            errors.Add(new ValidationError("Plan.NumberOfStudentsInvalid", "Number of students must be greater than zero."));
        }

        return errors;
    }

    public static PlanCatalogItem CreateDraft(PlanDraftInput input) =>
        new(
            Id: 0,
            input.Name.Trim(),
            input.PriceWithoutVat,
            input.LessonsPerWeek,
            input.DurationMinutes,
            input.Flexibility,
            input.NumberOfStudents,
            input.EffectiveFrom,
            input.RetireOn,
            PlanCatalogState.Draft);

    public static PlanCatalogItem UpdateDraft(PlanCatalogItem plan, PlanDraftInput input) =>
        plan with
        {
            Name = input.Name.Trim(),
            PriceWithoutVat = input.PriceWithoutVat,
            LessonsPerWeek = input.LessonsPerWeek,
            DurationMinutes = input.DurationMinutes,
            Flexibility = input.Flexibility,
            NumberOfStudents = input.NumberOfStudents,
            EffectiveFrom = input.EffectiveFrom,
            RetireOn = input.RetireOn
        };
}
