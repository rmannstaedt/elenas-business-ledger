using System.Globalization;
using System.Text.RegularExpressions;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Money;

namespace ElenasBusinessLedger.Infrastructure.Invoices.Pdf;

// Shapes the saved billing values into the service lines, lesson list, and payment details used by the PDF writer.
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

    // Groups lessons by plan and price, then formats the saved billing values for the Danish PDF layout.
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
            return new InvoicePdfServiceText($"Undervisning - {planName}", string.Empty);
        }

        var detail = match.Groups["detail"].Value
            .Replace("/uge", " pr. uge", StringComparison.Ordinal);
        return new InvoicePdfServiceText(
            $"Undervisning - {match.Groups["title"].Value}",
            $"Individuel undervisning, {detail}");
    }

    private static IReadOnlyList<string> CreatePlanChangeNotes(IReadOnlyList<InvoiceLineDraft> orderedLines)
    {
        var notes = new List<string>();
        for (var index = 1; index < orderedLines.Count; index++)
        {
            var previous = orderedLines[index - 1];
            var current = orderedLines[index];
            if (previous.PlanCatalogId == current.PlanCatalogId)
            {
                continue;
            }

            var date = current.LessonDateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            notes.Add(string.Equals(previous.PlanName, current.PlanName, StringComparison.Ordinal)
                ? $"Plan skiftede til en anden plan med navnet {current.PlanName} den {date}."
                : $"Plan skiftede fra {previous.PlanName} til {current.PlanName} den {date}.");
        }

        return notes;
    }
    private static IReadOnlyList<string> CreateIncludedLessons(IReadOnlyList<InvoiceLineDraft> orderedLines) =>
        orderedLines
            .Select(line => line.HasKnownLessonTime
                ? line.LessonDateTime.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)
                : line.LessonDateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture))
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

    // Groups lessons only when both their plan label and unit price match for the PDF service table.
    private sealed record ServiceGroupKey(string PlanName, Money PriceWithoutVat);

    // Carries the display title and detail line that the PDF writer uses for a grouped teaching service.
    private sealed record InvoicePdfServiceText(string Description, string Detail);
}

// Holds one grouped service's labels, quantities, and prices for the invoice PDF.
public sealed record InvoicePdfServiceLine(
    string Description,
    string Detail,
    int Quantity,
    string Unit,
    Money UnitPriceWithoutVat,
    Money TotalWithoutVat,
    DateTimeOffset FirstLessonDateTime);
