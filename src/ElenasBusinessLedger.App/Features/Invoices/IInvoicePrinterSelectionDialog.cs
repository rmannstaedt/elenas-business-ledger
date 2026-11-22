namespace ElenasBusinessLedger.App.Services;

// Lets the user choose an invoice printer without coupling settings logic to the Windows picker.
public interface IInvoicePrinterSelectionDialog
{
    string? SelectPrinterName();
}
