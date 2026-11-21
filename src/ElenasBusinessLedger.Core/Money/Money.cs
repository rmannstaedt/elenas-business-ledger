namespace ElenasBusinessLedger.Core.Money;

// Keeps an amount with its currency so billing arithmetic can preserve currency and rounding rules.
public readonly record struct Money(decimal Amount, string Currency)
{
    // Rounds Danish kroner to two decimal places, with midpoint values rounded away from zero.
    public static Money Dkk(decimal amount) => new(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), "DKK");

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal factor) =>
        new(decimal.Round(money.Amount * factor, 2, MidpointRounding.AwayFromZero), money.Currency);

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (!StringComparer.Ordinal.Equals(left.Currency, right.Currency))
        {
            throw new InvalidOperationException("Money values must use the same currency.");
        }
    }
}
