namespace ElenasBusinessLedger.Core.Invoices;

public readonly record struct InvoiceNumber(int Value)
{
    public override string ToString() => Value.ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
}
