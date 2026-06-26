using System.Globalization;
using System.Text.RegularExpressions;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Money;

namespace MySmallBusinessLedger.Infrastructure.Invoices.Pdf;

public sealed record InvoicePdfDocumentModel(
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string Period,
    string CompanyName,
    IReadOnlyList<string> CompanyAddressLines,
    string? CompanyCvr,
    string PaymentInstructions,
    string BankRegistrationNumber,
    string BankAccountNumber,
    string MobilePayAccount,
    string StudentFullName,
    IReadOnlyList<string> StudentAddressLines,
    IReadOnlyList<InvoicePdfServiceLine> Services,
    IReadOnlyList<string> IncludedLessons,
    IReadOnlyList<string> PlanChangeNotes,
    InvoiceTotals Totals,
    PriceAdjustment? PriceAdjustment)
{
    private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");

    public static InvoicePdfDocumentModel Create(
        InvoiceDraft draft,
        InvoiceTotals totals,
        InvoiceNumber invoiceNumber)
    {
        var orderedLines = draft.Lines.OrderBy(line => line.LessonDateTime).ToArray();
        var services = orderedLines
            .GroupBy(line => new ServiceGroupKey(line.PlanName, line.PriceWithoutVat))
            .Select(group =>
            {
                var orderedGroup = group.OrderBy(line => line.LessonDateTime).ToArray();
                var serviceText = CreateServiceText(group.Key.PlanName);
                return new InvoicePdfServiceLine(
                    serviceText.Description,
                    serviceText.Detail,
                    orderedGroup.Length,
                    "lektioner",
                    group.Key.PriceWithoutVat,
                    orderedGroup.Aggregate(Money.Dkk(0m), (sum, line) => sum + line.PriceWithoutVat),
                    orderedGroup[0].LessonDateTime);
            })
            .OrderBy(service => service.FirstLessonDateTime)
            .ToArray();

        return new InvoicePdfDocumentModel(
            invoiceNumber.ToString(),
            draft.InvoiceDate,
            FormatPeriod(draft.Period),
            draft.Company.Name,
            SplitLines(draft.Company.Address),
            draft.Company.Cvr,
            draft.Company.PaymentInstructions,
            draft.Company.BankRegistrationNumber,
            draft.Company.BankAccountNumber,
            draft.Company.MobilePayAccount,
            draft.Student.FullName,
            SplitLines(draft.Student.Address),
            services,
            CreateIncludedLessons(orderedLines),
            CreatePlanChangeNotes(orderedLines),
            totals,
            draft.PriceAdjustment);
    }

    private static InvoicePdfServiceText CreateServiceText(string planName)
    {
        var match = Regex.Match(
            planName,
            @"^(?<title>.+?)\s+-\s+\d{4}\s*:\s*(?<detail>.+)$",
            RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return new InvoicePdfServiceText($"Klaverundervisning - {planName}", string.Empty);
        }

        var detail = match.Groups["detail"].Value
            .Replace("/uge", " pr. uge", StringComparison.Ordinal);
        return new InvoicePdfServiceText(
            $"Klaverundervisning - {match.Groups["title"].Value}",
            $"Individuel undervisning, {detail}");
    }

    private static IReadOnlyList<string> CreatePlanChangeNotes(IReadOnlyList<InvoiceLineDraft> orderedLines)
    {
        var orderedPlanRuns = orderedLines
            .GroupBy(line => line.PlanName)
            .Select(group => new
            {
                PlanName = group.Key,
                FirstLesson = group.Min(line => line.LessonDateTime)
            })
            .OrderBy(group => group.FirstLesson)
            .ToArray();

        if (orderedPlanRuns.Length <= 1)
        {
            return [];
        }

        var notes = new List<string>();
        for (var index = 1; index < orderedPlanRuns.Length; index++)
        {
            notes.Add(
                "Plan skiftede fra "
                + orderedPlanRuns[index - 1].PlanName
                + " til "
                + orderedPlanRuns[index].PlanName
                + " den "
                + orderedPlanRuns[index].FirstLesson.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                + ".");
        }

        return notes;
    }

    private static IReadOnlyList<string> CreateIncludedLessons(IReadOnlyList<InvoiceLineDraft> orderedLines) =>
        orderedLines
            .Select(line => line.LessonDateTime.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture))
            .ToArray();

    private static string FormatPeriod(InvoicePeriod period)
    {
        if (period.From.Year == period.To.Year && period.From.Month == period.To.Month)
        {
            var text = period.From.ToString("MMMM yyyy", DanishCulture);
            return char.ToUpper(text[0], DanishCulture) + text[1..];
        }

        return period.From.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
            + " - "
            + period.To.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<string> SplitLines(string text) =>
        Regex.Replace(text, @",\s+(?=\d{4}\b)", "\n", RegexOptions.CultureInvariant)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private sealed record ServiceGroupKey(string PlanName, Money PriceWithoutVat);

    private sealed record InvoicePdfServiceText(string Description, string Detail);
}

public sealed record InvoicePdfServiceLine(
    string Description,
    string Detail,
    int Quantity,
    string Unit,
    Money UnitPriceWithoutVat,
    Money TotalWithoutVat,
    DateTimeOffset FirstLessonDateTime);
