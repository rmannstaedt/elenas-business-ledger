using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Core.Ports;

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

public sealed record EconomySettings(DateOnly FirstApplicationInvoiceMonth);

public sealed record EconomyManualMonthOverride(int Year, int Month, MoneyAmount Net, MoneyAmount Vat)
{
    public MoneyAmount Total => Net + Vat;
}
