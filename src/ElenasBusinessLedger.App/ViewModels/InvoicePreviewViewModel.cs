using System.Collections.ObjectModel;
using System.Globalization;
using ElenasBusinessLedger.App.Commands;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Money;
using ElenasBusinessLedger.Core.UseCases.Invoices;

namespace ElenasBusinessLedger.App.ViewModels;

public sealed class InvoicePreviewViewModel : ViewModelBase
{
    private static readonly TimeSpan DefaultExtendedIssueWaitPromptDelay = TimeSpan.FromSeconds(7);

    private readonly IInvoiceWorkflowService invoiceWorkflowService;
    private readonly Func<CancellationToken, Task<bool>>? ensureInvoicePrinterConfiguredAsync;
    private readonly Func<CancellationToken, Task<bool>> confirmContinueWaitingAsync;
    private readonly TimeSpan extendedIssueWaitPromptDelay;
    private readonly Func<PendingInvoiceRecovery, CancellationToken, Task<InvoiceRecoveryChoice>>? chooseRecovery;
    private readonly InvoicePreviewResult preview;
    private string priceAdjustmentAmountText = string.Empty;
    private string priceAdjustmentReason = string.Empty;
    private InvoiceTotals totals;
    private string? errorMessage;
    private bool isBusy;
    private bool isChoosingRecovery;

    public InvoicePreviewViewModel(
        IInvoiceWorkflowService invoiceWorkflowService,
        InvoicePreviewResult preview,
        Func<CancellationToken, Task<bool>>? ensureInvoicePrinterConfiguredAsync = null,
        Func<CancellationToken, Task<bool>>? confirmContinueWaitingAsync = null,
        TimeSpan? extendedIssueWaitPromptDelay = null,
        Func<PendingInvoiceRecovery, CancellationToken, Task<InvoiceRecoveryChoice>>? chooseRecovery = null)
    {
        this.invoiceWorkflowService = invoiceWorkflowService;
        this.ensureInvoicePrinterConfiguredAsync = ensureInvoicePrinterConfiguredAsync;
        this.confirmContinueWaitingAsync = confirmContinueWaitingAsync ?? (_ => Task.FromResult(true));
        this.extendedIssueWaitPromptDelay = extendedIssueWaitPromptDelay ?? DefaultExtendedIssueWaitPromptDelay;
        this.preview = preview;
        this.chooseRecovery = chooseRecovery;
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

    public bool CanDismiss => !IsBusy;

    public string BusyStatusText => IsBusy
        ? "Writing and printing invoice. Please keep this window open until it finishes."
        : string.Empty;

    public bool HasBusyStatus => IsBusy;

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                OnPropertyChanged(nameof(CanDismiss));
                OnPropertyChanged(nameof(BusyStatusText));
                OnPropertyChanged(nameof(HasBusyStatus));
                PrintInvoiceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool WasIssued { get; private set; }

    public AsyncRelayCommand PrintInvoiceCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public Task<bool> RequestDismissAsync()
    {
        if (CanDismiss)
        {
            return Task.FromResult(true);
        }

        ErrorMessage = "The invoice is still being written and printed. Please keep this window open until it finishes.";
        return Task.FromResult(false);
    }

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

            if (ensureInvoicePrinterConfiguredAsync is not null &&
                !await ensureInvoicePrinterConfiguredAsync(CancellationToken.None))
            {
                ErrorMessage = "Choose an invoice printer before printing the invoice.";
                return;
            }

            var issue = await IssueWithExtendedWaitPromptAsync(draft);
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

    private async Task<OperationResult<IssueInvoiceResult>> IssueWithExtendedWaitPromptAsync(InvoiceDraft draft)
    {
        var issueTask = invoiceWorkflowService.IssueAsync(draft, CancellationToken.None, ChooseRecoveryAsync);
        while (true)
        {
            var completedTask = await Task.WhenAny(
                issueTask,
                Task.Delay(extendedIssueWaitPromptDelay));
            if (ReferenceEquals(completedTask, issueTask))
            {
                return await issueTask;
            }

            if (isChoosingRecovery)
            {
                continue;
            }

            var keepWaiting = await confirmContinueWaitingAsync(CancellationToken.None);
            if (!keepWaiting)
            {
                ErrorMessage =
                    "The invoice is still being written and printed. It cannot be safely cancelled from this window.";
            }
        }
    }

    private async Task<InvoiceRecoveryChoice> ChooseRecoveryAsync(
        PendingInvoiceRecovery recovery, CancellationToken cancellationToken)
    {
        if (chooseRecovery is null)
        {
            return InvoiceRecoveryChoice.Cancel;
        }

        isChoosingRecovery = true;
        try
        {
            return await chooseRecovery(recovery, cancellationToken);
        }
        finally
        {
            isChoosingRecovery = false;
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
