namespace ElenaSmallBusinessLedger.Core.Invoices;

public readonly record struct InvoiceNumber(int Value)
{
    public override string ToString() => Value.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
}
