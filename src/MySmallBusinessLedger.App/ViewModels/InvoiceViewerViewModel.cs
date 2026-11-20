using System.Collections.ObjectModel;
using System.Globalization;
using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Money;

namespace MySmallBusinessLedger.App.ViewModels;

public sealed class InvoiceViewerViewModel : ViewModelBase
{
    private readonly IInvoiceWorkflowService? invoiceWorkflowService;
    private readonly Func<CancellationToken, Task<bool>>? ensureInvoicePrinterConfiguredAsync;
    private readonly InvoiceNumber invoiceNumber;
    private InvoicePrintStatus printStatus;
    private DateTimeOffset? printedAt;
    private string? lastPrintError;
    private string? errorMessage;
    private bool isBusy;

    public InvoiceViewerViewModel(
        IssuedInvoice invoice,
        IInvoiceWorkflowService? invoiceWorkflowService = null,
        Func<CancellationToken, Task<bool>>? ensureInvoicePrinterConfiguredAsync = null)
    {
        this.invoiceWorkflowService = invoiceWorkflowService;
        this.ensureInvoicePrinterConfiguredAsync = ensureInvoicePrinterConfiguredAsync;
        invoiceNumber = invoice.InvoiceNumber;
        printStatus = invoice.PrintStatus;
        printedAt = invoice.PrintedAt;
        lastPrintError = invoice.LastPrintError;

        InvoiceNumberText = invoice.InvoiceNumber.ToString();
        InvoiceDateText = invoice.InvoiceDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        CompanyName = invoice.Company.Name;
        CompanyAddress = invoice.Company.Address;
        StudentName = invoice.Student.FullName;
        StudentAddress = invoice.Student.Address;
        TotalWithoutVatText = FormatMoney(invoice.Totals.TotalWithoutVat);
        VatAmountText = FormatMoney(invoice.Totals.VatAmount);
        TotalText = FormatMoney(invoice.Totals.Total);
        PdfFilePath = invoice.PdfFilePath ?? string.Empty;

        foreach (var line in invoice.Lines.OrderBy(line => line.LessonDateTime))
        {
            Lines.Add(new InvoiceViewerLineDisplayModel(line));
        }

        ReprintInvoiceCommand = new AsyncRelayCommand(ReprintInvoiceAsync, () => CanReprintInvoice);
    }

    public string InvoiceNumberText { get; }

    public string InvoiceDateText { get; }

    public string CompanyName { get; }

    public string CompanyAddress { get; }

    public string StudentName { get; }

    public string StudentAddress { get; }

    public string TotalWithoutVatText { get; }

    public string VatAmountText { get; }

    public string TotalText { get; }

    public string PdfFilePath { get; }

    public string PrintStatusText => printStatus switch
    {
        InvoicePrintStatus.Printed => printedAt is null
            ? "Print request sent"
            : $"Print request sent {printedAt.Value:dd-MM-yyyy HH:mm}",
        InvoicePrintStatus.PrintFailed => "Print failed",
        _ => "Not printed"
    };

    public string? LastPrintError => lastPrintError;

    public bool HasLastPrintError => !string.IsNullOrWhiteSpace(LastPrintError);

    public string? ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                OnPropertyChanged(nameof(CanReprintInvoice));
                ReprintInvoiceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanReprintInvoice =>
        !IsBusy &&
        invoiceWorkflowService is not null &&
        !string.IsNullOrWhiteSpace(PdfFilePath);

    public ObservableCollection<InvoiceViewerLineDisplayModel> Lines { get; } = [];

    public AsyncRelayCommand ReprintInvoiceCommand { get; }

    private async Task ReprintInvoiceAsync()
    {
        if (invoiceWorkflowService is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            ErrorMessage = null;
            if (ensureInvoicePrinterConfiguredAsync is not null &&
                !await ensureInvoicePrinterConfiguredAsync(CancellationToken.None))
            {
                ErrorMessage = "Choose an invoice printer before reprinting the invoice.";
                return;
            }

            var reprint = await invoiceWorkflowService.ReprintAsync(invoiceNumber, CancellationToken.None);
            if (!reprint.Succeeded)
            {
                ErrorMessage = reprint.Errors.FirstOrDefault()?.Message ?? "The invoice could not be reprinted.";
                return;
            }

            ApplyPrintState(reprint.Value!.Invoice);
            if (!reprint.Value.PrintSucceeded)
            {
                ErrorMessage = reprint.Value.PrintErrors.FirstOrDefault()?.Message ??
                    "The invoice is still issued, but the print request failed.";
                return;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyPrintState(IssuedInvoice invoice)
    {
        printStatus = invoice.PrintStatus;
        printedAt = invoice.PrintedAt;
        lastPrintError = invoice.LastPrintError;
        OnPropertyChanged(nameof(PrintStatusText));
        OnPropertyChanged(nameof(LastPrintError));
        OnPropertyChanged(nameof(HasLastPrintError));
    }

    private static string FormatMoney(Money money) =>
        string.Create(CultureInfo.GetCultureInfo("da-DK"), $"{money.Amount:N2} {money.Currency}");
}

public sealed class InvoiceViewerLineDisplayModel
{
    public InvoiceViewerLineDisplayModel(IssuedInvoiceLine line)
    {
        LessonDateTimeText = line.HasKnownLessonTime
            ? line.LessonDateTime.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)
            : line.LessonDateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        PlanName = line.PlanName;
        PriceWithoutVatText = string.Create(
            CultureInfo.GetCultureInfo("da-DK"),
            $"{line.PriceWithoutVat.Amount:N2} {line.PriceWithoutVat.Currency}");
    }

    public string LessonDateTimeText { get; }

    public string PlanName { get; }

    public string PriceWithoutVatText { get; }
}
