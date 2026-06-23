using System.Windows.Controls;

namespace MySmallBusinessLedger.App.Services;

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
