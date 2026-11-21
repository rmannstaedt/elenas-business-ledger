namespace ElenasBusinessLedger.Core.Invoices;

// Gives invoice references a numeric identity and a consistent display format.
public readonly record struct InvoiceNumber(int Value)
{
    public override string ToString() => Value.ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
}
