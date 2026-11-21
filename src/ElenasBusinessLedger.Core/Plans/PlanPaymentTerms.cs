namespace ElenasBusinessLedger.Core.Plans;

public static class PlanPaymentTerms
{
    public const string PaidMonthlyInAdvance = "Paid monthly in advance";
    public const string PaidPerLesson = "Paid per lesson";

    public static IReadOnlyList<string> All { get; } =
        [PaidMonthlyInAdvance, PaidPerLesson];

    public static bool IsSupported(string paymentTerms) =>
        All.Contains(paymentTerms, StringComparer.Ordinal);
}
