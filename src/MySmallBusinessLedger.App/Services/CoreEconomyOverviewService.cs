using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.UseCases.Economy;
using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.App.Services;

public sealed class CoreEconomyOverviewService : IEconomyOverviewService
{
    private readonly EconomyOverviewQuery query;
    private readonly IEconomyRepository? economyRepository;
    private readonly IInvoiceRepository? invoiceRepository;

    public CoreEconomyOverviewService(
        EconomyOverviewQuery query,
        IEconomyRepository? economyRepository = null,
        IInvoiceRepository? invoiceRepository = null)
    {
        this.query = query;
        this.economyRepository = economyRepository;
        this.invoiceRepository = invoiceRepository;
    }

    public async Task<OperationResult<EconomyOverview>> LoadAsync(int year, CancellationToken cancellationToken)
    {
        try
        {
            return await query.LoadAsync(year, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<EconomyOverview>.Failure(
                new ValidationError("EconomyOverview.LoadFailed", ex.Message));
        }
    }

    public async Task<OperationResult<EconomyOverview>> SaveManualMonthOverridesAsync(
        int overviewYear,
        EconomySettings settings,
        IReadOnlyList<EconomyManualMonthOverride> overrides,
        CancellationToken cancellationToken)
    {
        if (economyRepository is null)
        {
            return OperationResult<EconomyOverview>.Failure(
                new ValidationError("EconomyOverview.ManualOverridesUnavailable", "Manual month totals cannot be saved."));
        }

        try
        {
            var safeOverrides = await CreateSafeManualOverridesAsync(overviewYear, settings, overrides, cancellationToken);
            await economyRepository.SaveSettingsAndManualMonthOverridesAsync(settings, safeOverrides, cancellationToken);
            return await query.LoadAsync(overviewYear, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<EconomyOverview>.Failure(
                new ValidationError("EconomyOverview.SaveManualOverridesFailed", ex.Message));
        }
    }

    private async Task<IReadOnlyList<EconomyManualMonthOverride>> CreateSafeManualOverridesAsync(
        int overviewYear,
        EconomySettings settings,
        IReadOnlyList<EconomyManualMonthOverride> overrides,
        CancellationToken cancellationToken)
    {
        if (invoiceRepository is null)
        {
            return overrides;
        }

        var cutOver = settings.FirstApplicationInvoiceMonth;
        var candidateOverrides = overrides
            .Where(manual => new DateOnly(manual.Year, manual.Month, 1) < cutOver)
            .GroupBy(manual => (manual.Year, manual.Month))
            .ToDictionary(group => group.Key, group => group.Last());
        var yearsToInspect = candidateOverrides.Keys
            .Select(key => key.Year)
            .Append(overviewYear)
            .Distinct()
            .Order()
            .ToArray();
        var issuedMonths = new HashSet<(int Year, int Month)>();
        foreach (var year in yearsToInspect)
        {
            var invoices = await invoiceRepository.ListIssuedInvoiceOverviewRowsAsync(year, cancellationToken);
            foreach (var invoice in invoices)
            {
                var monthStart = new DateOnly(invoice.Period.From.Year, invoice.Period.From.Month, 1);
                if (monthStart < cutOver)
                {
                    issuedMonths.Add((monthStart.Year, monthStart.Month));
                }
            }
        }

        var safeOverrides = candidateOverrides
            .Where(candidate => !issuedMonths.Contains(candidate.Key))
            .Select(candidate => candidate.Value)
            .ToList();
        safeOverrides.AddRange(issuedMonths
            .OrderBy(month => month.Year)
            .ThenBy(month => month.Month)
            .Select(month => new EconomyManualMonthOverride(
                month.Year,
                month.Month,
                MoneyAmount.Dkk(0m),
                MoneyAmount.Dkk(0m))));
        return safeOverrides;
    }
}
