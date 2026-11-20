using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;
using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Core.UseCases.Economy;

public sealed class EconomyOverviewQuery
{
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IEconomyRepository? economyRepository;

    public EconomyOverviewQuery(IInvoiceRepository invoiceRepository)
        : this(invoiceRepository, economyRepository: null)
    {
    }

    public EconomyOverviewQuery(IInvoiceRepository invoiceRepository, IEconomyRepository? economyRepository)
    {
        this.invoiceRepository = invoiceRepository;
        this.economyRepository = economyRepository;
    }

    public async Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken)
    {
        var years = new List<EconomyOverviewYearTotal>();
        var fromYear = year - 4;
        var settings = economyRepository is null
            ? null
            : await economyRepository.GetSettingsAsync(cancellationToken);
        var firstApplicationInvoiceMonth = settings?.FirstApplicationInvoiceMonth;
        var manualOverrides = economyRepository is null
            ? Array.Empty<EconomyManualMonthOverride>()
            : await economyRepository.ListManualMonthOverridesAsync(fromYear, year, cancellationToken);
        var manualOverridesByMonth = manualOverrides.ToDictionary(
            manual => (manual.Year, manual.Month),
            manual => manual);

        var validationErrors = new List<ValidationError>();
        AddIfNotNull(validationErrors, ValidateCutOverConsistency(firstApplicationInvoiceMonth, manualOverrides));

        IReadOnlyList<EconomyInvoiceRecord> currentYearRecords = [];
        IReadOnlyList<EconomyOverviewMonthTotal> currentYearMonths = [];
        for (var offset = 0; offset < 5; offset++)
        {
            var summaryYear = year - offset;
            var records = await invoiceRepository.ListIssuedInvoiceOverviewRowsAsync(summaryYear, cancellationToken);
            var orderedYearRecords = records
                .OrderBy(record => record.InvoiceNumber.Value)
                .ToArray();

            AddIfNotNull(validationErrors, ValidateManualIssuedMonthConflict(
                orderedYearRecords,
                manualOverridesByMonth));

            var yearMonths = CreateMonthTotals(
                summaryYear,
                orderedYearRecords,
                manualOverridesByMonth,
                firstApplicationInvoiceMonth);
            years.Add(CreateYearTotal(summaryYear, yearMonths));
            if (offset == 0)
            {
                currentYearRecords = orderedYearRecords;
                currentYearMonths = yearMonths;
            }
        }

        var invoices = currentYearRecords
            .Select(record => new EconomyOverviewInvoiceRow(
                record.InvoiceNumber,
                record.InvoiceDate,
                record.Period,
                record.PdfFilePath,
                GetFileName(record.PdfFilePath),
                record.Totals.TotalWithoutVat,
                record.Totals.VatAmount,
                record.Totals.Total))
            .ToArray();
        var quarters = Enumerable.Range(1, 4)
            .Select(quarter => CreateQuarterTotal(
                quarter,
                currentYearMonths.Where(month => GetQuarter(month.Month) == quarter).ToArray()))
            .ToArray();
        var yearToDate = CreateYearToDateTotal(currentYearMonths);

        var overview = new EconomyOverview(
            year,
            invoices,
            currentYearMonths,
            quarters,
            yearToDate,
            years,
            firstApplicationInvoiceMonth);
        return validationErrors.Count == 0
            ? OperationResult<EconomyOverview>.Success(overview)
            : OperationResult<EconomyOverview>.Failure(overview, validationErrors.ToArray());
    }

    private static IReadOnlyList<EconomyOverviewMonthTotal> CreateMonthTotals(
        int year,
        IReadOnlyList<EconomyInvoiceRecord> records,
        IReadOnlyDictionary<(int Year, int Month), EconomyManualMonthOverride> manualOverridesByMonth,
        DateOnly? firstApplicationInvoiceMonth) =>
        Enumerable.Range(1, 12)
            .Select(month => CreateMonthTotal(
                year,
                month,
                records.Where(record => record.Period.From.Month == month).ToArray(),
                manualOverridesByMonth,
                firstApplicationInvoiceMonth))
            .ToArray();

    private static EconomyOverviewMonthTotal CreateMonthTotal(
        int year,
        int month,
        IReadOnlyList<EconomyInvoiceRecord> records,
        IReadOnlyDictionary<(int Year, int Month), EconomyManualMonthOverride> manualOverridesByMonth,
        DateOnly? firstApplicationInvoiceMonth)
    {
        var monthStart = new DateOnly(year, month, 1);
        var isLegacyManualMonth = firstApplicationInvoiceMonth is not null &&
            monthStart < firstApplicationInvoiceMonth.Value;
        if (isLegacyManualMonth)
        {
            if (manualOverridesByMonth.TryGetValue((year, month), out var manual))
            {
                return new EconomyOverviewMonthTotal(
                    month,
                    HasInvoices: false,
                    manual.Net,
                    manual.Vat,
                    manual.Total,
                    EconomyOverviewMonthSource.ManualOverride,
                    IsLegacyManualMonth: true);
            }

            return CreateIssuedInvoiceMonthTotal(month, records, isLegacyManualMonth: true);
        }

        return CreateIssuedInvoiceMonthTotal(month, records, isLegacyManualMonth: false);
    }

    private static ValidationError? ValidateManualIssuedMonthConflict(
        IReadOnlyList<EconomyInvoiceRecord> records,
        IReadOnlyDictionary<(int Year, int Month), EconomyManualMonthOverride> manualOverridesByMonth)
    {
        var conflictingInvoice = records.FirstOrDefault(record =>
            manualOverridesByMonth.ContainsKey((record.Period.From.Year, record.Period.From.Month)));
        return conflictingInvoice is null
            ? null
            : new ValidationError(
                "EconomyOverview.ManualOverrideIssuedInvoiceConflict",
                $"Manual month totals for {FormatMonth(conflictingInvoice.Period.From)} conflict with Invoice {conflictingInvoice.InvoiceNumber}. Remove either the manual month total or the application-issued invoice for that month.");
    }

    private static EconomyOverviewMonthTotal CreateEmptyLegacyMonthTotal(int month) =>
        new(
            month,
            HasInvoices: false,
            null,
            null,
            null,
            EconomyOverviewMonthSource.None,
            IsLegacyManualMonth: true);

    private static EconomyOverviewMonthTotal CreateEmptyApplicationMonthTotal(int month) =>
        new(
            month,
            HasInvoices: false,
            null,
            null,
            null,
            EconomyOverviewMonthSource.None);

    private static EconomyOverviewMonthTotal CreateIssuedInvoiceMonthTotal(
        int month,
        IReadOnlyList<EconomyInvoiceRecord> records,
        bool isLegacyManualMonth)
    {
        var totals = Sum(records);
        return totals is null
            ? isLegacyManualMonth
                ? CreateEmptyLegacyMonthTotal(month)
                : CreateEmptyApplicationMonthTotal(month)
            : new EconomyOverviewMonthTotal(
                    month,
                    HasInvoices: true,
                    totals.Net,
                    totals.Vat,
                    totals.Total,
                    EconomyOverviewMonthSource.IssuedInvoices,
                    isLegacyManualMonth);
    }

    private static EconomyOverviewQuarterTotal CreateQuarterTotal(int quarter, IReadOnlyList<EconomyOverviewMonthTotal> months)
    {
        var totals = Sum(months);
        return new EconomyOverviewQuarterTotal(
            quarter,
            months.Any(month => month.HasInvoices),
            totals?.Net,
            totals?.Vat,
            totals?.Total);
    }

    private static EconomyOverviewYearToDateTotal CreateYearToDateTotal(IReadOnlyList<EconomyOverviewMonthTotal> months)
    {
        var totals = Sum(months);
        return totals is null
            ? new EconomyOverviewYearToDateTotal(MoneyAmount.Dkk(0m), MoneyAmount.Dkk(0m), MoneyAmount.Dkk(0m))
            : new EconomyOverviewYearToDateTotal(totals.Net, totals.Vat, totals.Total);
    }

    private static EconomyOverviewYearTotal CreateYearTotal(int year, IReadOnlyList<EconomyOverviewMonthTotal> months)
    {
        var totals = Sum(months);
        return new EconomyOverviewYearTotal(
            year,
            months.Any(month => month.HasInvoices),
            totals?.Net,
            totals?.Vat,
            totals?.Total);
    }

    private static EconomyOverviewYearToDateTotal? Sum(IReadOnlyList<EconomyInvoiceRecord> records)
    {
        if (records.Count == 0)
        {
            return null;
        }

        var net = records[0].Totals.TotalWithoutVat;
        var vat = records[0].Totals.VatAmount;
        var total = records[0].Totals.Total;
        foreach (var record in records.Skip(1))
        {
            net += record.Totals.TotalWithoutVat;
            vat += record.Totals.VatAmount;
            total += record.Totals.Total;
        }

        return new EconomyOverviewYearToDateTotal(net, vat, total);
    }

    private static EconomyOverviewYearToDateTotal? Sum(IReadOnlyList<EconomyOverviewMonthTotal> months)
    {
        var monthsWithTotals = months
            .Where(month => month.Net is not null && month.Vat is not null && month.Total is not null)
            .ToArray();
        if (monthsWithTotals.Length == 0)
        {
            return null;
        }

        var net = monthsWithTotals[0].Net!.Value;
        var vat = monthsWithTotals[0].Vat!.Value;
        var total = monthsWithTotals[0].Total!.Value;
        foreach (var month in monthsWithTotals.Skip(1))
        {
            net += month.Net!.Value;
            vat += month.Vat!.Value;
            total += month.Total!.Value;
        }

        return new EconomyOverviewYearToDateTotal(net, vat, total);
    }

    private static ValidationError? ValidateCutOverConsistency(
        DateOnly? firstApplicationInvoiceMonth,
        IReadOnlyList<EconomyManualMonthOverride> manualOverrides)
    {
        if (manualOverrides.Count == 0)
        {
            return null;
        }

        if (firstApplicationInvoiceMonth is null)
        {
            return new ValidationError(
                "EconomyOverview.CutOverMissing",
                "Set the first application month in Application Settings before using manual month totals.");
        }

        var conflictingManualOverride = manualOverrides.FirstOrDefault(manual =>
            new DateOnly(manual.Year, manual.Month, 1) >= firstApplicationInvoiceMonth.Value);
        var conflictingMonth = conflictingManualOverride is null
            ? default
            : new DateOnly(conflictingManualOverride.Year, conflictingManualOverride.Month, 1);
        return conflictingManualOverride is null
            ? null
            : new ValidationError(
                "EconomyOverview.ManualOverrideAfterCutOver",
                $"Manual month totals for {FormatMonth(conflictingMonth)} are on or after the first application month {FormatMonth(firstApplicationInvoiceMonth.Value)}. Manual totals are only allowed before that month.");
    }

    private static void AddIfNotNull(List<ValidationError> errors, ValidationError? error)
    {
        if (error is not null)
        {
            errors.Add(error);
        }
    }

    private static string FormatMonth(DateOnly month) =>
        month.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);

    private static int GetQuarter(int month) =>
        ((month - 1) / 3) + 1;

    private static string GetFileName(string path)
    {
        var normalized = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFileName(normalized);
    }
}
