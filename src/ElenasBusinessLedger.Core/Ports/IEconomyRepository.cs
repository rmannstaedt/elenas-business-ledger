using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;

namespace ElenasBusinessLedger.Core.Ports;

// Persists the reporting cutover and legacy amounts used alongside application-issued invoices.
public interface IEconomyRepository
{
    Task<EconomySettings?> GetSettingsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<EconomyManualMonthOverride>> ListManualMonthOverridesAsync(
        int fromYearInclusive,
        int toYearInclusive,
        CancellationToken cancellationToken);

    Task SaveSettingsAsync(EconomySettings settings, CancellationToken cancellationToken);

    Task SaveManualMonthOverridesAsync(
        IReadOnlyList<EconomyManualMonthOverride> overrides,
        CancellationToken cancellationToken);

    Task SaveSettingsAndManualMonthOverridesAsync(
        EconomySettings settings,
        IReadOnlyList<EconomyManualMonthOverride> overrides,
        CancellationToken cancellationToken);
}

// Holds the first application-backed invoice month so Economy knows which months can use manual totals.
public sealed record EconomySettings(DateOnly FirstApplicationInvoiceMonth);

// Holds manual net and VAT amounts for a legacy month that Economy cannot derive from issued invoices.
public sealed record EconomyManualMonthOverride(int Year, int Month, MoneyAmount Net, MoneyAmount Vat)
{
    public MoneyAmount Total => Net + Vat;
}
