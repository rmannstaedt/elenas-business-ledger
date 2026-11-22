using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

// Keeps draft and publication rules together so incomplete plans can be edited before going live.
internal static class PlanCatalogValidation
{
    // Checks the lesson terms that both drafts and published plans must satisfy.
    public static IReadOnlyList<ValidationError> ValidateDraft(PlanDraftInput input) =>
        PresentErrors(
            ValidatePrice(input.PriceWithoutVat),
            ValidateFrequency(input.LessonsPerWeek),
            ValidateDuration(input.DurationMinutes),
            ValidateFlexibility(input.Flexibility),
            ValidateStudentCount(input.NumberOfStudents),
            ValidatePaymentTerms(input.PaymentTerms?.Trim()));

    // Adds required-field checks before a draft can become assignable.
    public static IReadOnlyList<ValidationError> ValidatePublish(PlanCatalogItem plan) =>
        PresentErrors(
            string.IsNullOrWhiteSpace(plan.Name)
                ? new ValidationError("Plan.NameRequired", "Plan name is required.")
                : null,
            plan.PriceWithoutVat is null
                ? new ValidationError("Plan.PriceRequired", "Plan price is required.")
                : ValidatePrice(plan.PriceWithoutVat),
            ValidateFrequency(plan.LessonsPerWeek),
            ValidateDuration(plan.DurationMinutes),
            ValidateStudentCount(plan.NumberOfStudents),
            ValidateFlexibility(plan.Flexibility),
            string.IsNullOrWhiteSpace(plan.PaymentTerms)
                ? new ValidationError("Plan.PaymentTermsRequired", "Plan payment terms are required.")
                : ValidatePaymentTerms(plan.PaymentTerms));

    private static ValidationError? ValidatePrice(decimal? price) =>
        price < 0m ? new("Plan.PriceInvalid", "Plan price cannot be negative.") : null;

    private static ValidationError? ValidateFrequency(LessonsPerWeek frequency) =>
        !Enum.IsDefined(frequency) ? new("Plan.LessonsPerWeekInvalid", "Lessons per week is invalid.") : null;

    private static ValidationError? ValidateDuration(int minutes) =>
        minutes <= 0 ? new("Plan.DurationInvalid", "Plan duration must be greater than zero.") : null;

    private static ValidationError? ValidateFlexibility(PlanFlexibility flexibility) =>
        !Enum.IsDefined(flexibility) ? new("Plan.FlexibilityInvalid", "Plan flexibility is invalid.") : null;

    private static ValidationError? ValidateStudentCount(int count) =>
        count <= 0 ? new("Plan.NumberOfStudentsInvalid", "Number of students must be greater than zero.") : null;

    private static ValidationError? ValidatePaymentTerms(string? terms) =>
        !string.IsNullOrWhiteSpace(terms) && !PlanPaymentTerms.IsSupported(terms)
            ? new("Plan.PaymentTermsInvalid", "Plan payment terms are invalid.")
            : null;

    private static IReadOnlyList<ValidationError> PresentErrors(params ValidationError?[] errors) =>
        errors.OfType<ValidationError>().ToArray();
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
            input.PaymentTerms.Trim(),
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
            PaymentTerms = input.PaymentTerms.Trim(),
            RetireOn = input.RetireOn
        };
}
