using System.Collections.ObjectModel;
using System.Globalization;
using ElenaSmallBusinessLedger.App.Commands;
using ElenaSmallBusinessLedger.App.Services;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Money;
using ElenaSmallBusinessLedger.Core.UseCases.Invoices;

namespace ElenaSmallBusinessLedger.App.ViewModels;

public sealed class InvoicePreviewViewModel : ViewModelBase
{
    private readonly IInvoiceWorkflowService invoiceWorkflowService;
    private readonly InvoicePreviewResult preview;
    private string priceAdjustmentAmountText = string.Empty;
    private string priceAdjustmentReason = string.Empty;
    private InvoiceTotals totals;
    private string? errorMessage;
    private bool isBusy;

    public InvoicePreviewViewModel(IInvoiceWorkflowService invoiceWorkflowService, InvoicePreviewResult preview)
    {
        this.invoiceWorkflowService = invoiceWorkflowService;
        this.preview = preview;
        totals = preview.Totals;

        foreach (var line in preview.Draft.Lines)
        {
            var displayLine = new InvoiceLineDisplayModel(line);
            displayLine.InclusionChanged += (_, _) => _ = RefreshTotalsAsync();
            Lines.Add(displayLine);
        }

        PrintInvoiceCommand = new AsyncRelayCommand(PrintInvoiceAsync, () => !IsBusy);
    }

    public ObservableCollection<InvoiceLineDisplayModel> Lines { get; } = [];

    public string InvoiceDateText => preview.Draft.InvoiceDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    public string CompanyName => preview.Draft.Company.Name;

    public string CompanyAddress => preview.Draft.Company.Address;

    public string StudentName => preview.Draft.Student.FullName;

    public string StudentAddress => preview.Draft.Student.Address;

    public string PriceAdjustmentAmountText
    {
        get => priceAdjustmentAmountText;
        set
        {
            if (SetProperty(ref priceAdjustmentAmountText, value))
            {
                _ = RefreshTotalsAsync();
            }
        }
    }

    public string PriceAdjustmentReason
    {
        get => priceAdjustmentReason;
        set
        {
            if (SetProperty(ref priceAdjustmentReason, value))
            {
                _ = RefreshTotalsAsync();
            }
        }
    }

    public string TotalWithoutVatText => FormatMoney(totals.TotalWithoutVat);

    public string VatAmountText => FormatMoney(totals.VatAmount);

    public string TotalText => FormatMoney(totals.Total);

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
                PrintInvoiceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool WasIssued { get; private set; }

    public AsyncRelayCommand PrintInvoiceCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public async Task RefreshTotalsAsync()
    {
        var draft = BuildDraft(showErrors: false);
        if (draft is null)
        {
            return;
        }

        var result = await invoiceWorkflowService.CalculateTotalsAsync(draft, CancellationToken.None);
        if (!result.Succeeded)
        {
            return;
        }

        totals = result.Value!;
        OnPropertyChanged(nameof(TotalWithoutVatText));
        OnPropertyChanged(nameof(VatAmountText));
        OnPropertyChanged(nameof(TotalText));
    }

    private async Task PrintInvoiceAsync()
    {
        IsBusy = true;
        try
        {
            var draft = BuildDraft(showErrors: true);
            if (draft is null)
            {
                return;
            }

            var issue = await invoiceWorkflowService.IssueAsync(draft, CancellationToken.None);
            if (!issue.Succeeded)
            {
                ErrorMessage = issue.Errors.FirstOrDefault()?.Message ?? "Could not write the invoice.";
                return;
            }

            WasIssued = true;
            if (!issue.Value!.PrintSucceeded)
            {
                ErrorMessage = issue.Value.PrintErrors.FirstOrDefault()?.Message ??
                    "The invoice PDF was saved, but the print request failed.";
                return;
            }

            ErrorMessage = null;
            CloseRequested?.Invoke(this, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private InvoiceDraft? BuildDraft(bool showErrors)
    {
        var adjustment = ParsePriceAdjustment(showErrors, out var adjustmentIsValid);
        if (!adjustmentIsValid)
        {
            return null;
        }

        var includedLines = Lines
            .Where(line => line.IsIncluded)
            .Select(line => line.Line)
            .ToArray();
        if (includedLines.Length == 0)
        {
            if (showErrors)
            {
                ErrorMessage = "An invoice requires at least one lesson.";
            }

            return null;
        }

        var result = InvoiceDraft.TryCreate(
            preview.Draft.InvoiceDate,
            preview.Draft.Company,
            preview.Draft.Student,
            includedLines,
            adjustment);

        if (!result.Succeeded)
        {
            if (showErrors)
            {
                ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "The invoice is not valid.";
            }

            return null;
        }

        if (showErrors)
        {
            ErrorMessage = null;
        }

        return result.Value!;
    }

    private PriceAdjustment? ParsePriceAdjustment(bool showErrors, out bool isValid)
    {
        isValid = true;
        if (showErrors)
        {
            ErrorMessage = null;
        }

        if (string.IsNullOrWhiteSpace(PriceAdjustmentAmountText))
        {
            return null;
        }

        if (!decimal.TryParse(
            PriceAdjustmentAmountText,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out var amount))
        {
            if (showErrors)
            {
                ErrorMessage = "Price adjustment must be a number.";
            }

            isValid = false;
            return null;
        }

        if (string.IsNullOrWhiteSpace(PriceAdjustmentReason))
        {
            if (showErrors)
            {
                ErrorMessage = "A price adjustment requires a reason.";
            }

            isValid = false;
            return null;
        }

        return new PriceAdjustment(Money.Dkk(amount), PriceAdjustmentReason.Trim());
    }

    private static string FormatMoney(Money money) =>
        string.Create(CultureInfo.GetCultureInfo("da-DK"), $"{money.Amount:N2} {money.Currency}");
}
