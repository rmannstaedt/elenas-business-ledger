using System.Windows.Controls;

namespace ElenasBusinessLedger.App.Services;

// Uses the Windows printer picker to obtain the printer that Settings will remember.
public sealed class WindowsInvoicePrinterSelectionDialog : IInvoicePrinterSelectionDialog
{
    public string? SelectPrinterName()
    {
        var dialog = new PrintDialog();
        return dialog.ShowDialog() == true
            ? dialog.PrintQueue?.FullName
            : null;
    }
}
