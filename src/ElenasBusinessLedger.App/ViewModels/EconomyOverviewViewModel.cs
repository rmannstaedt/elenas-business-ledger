using System.Collections.ObjectModel;
using System.Globalization;
using ElenasBusinessLedger.App.Commands;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Money;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.UseCases.Economy;

namespace ElenasBusinessLedger.App.ViewModels;

// Manages report navigation, export, invoice opening, and the session controls for legacy month edits.
public sealed class EconomyOverviewViewModel : ViewModelBase
{
    private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");
    private readonly IEconomyOverviewService service;
    private readonly IEconomyOverviewExportDialog exportDialog;
    private readonly IEconomyOverviewCsvFileService csvFileService;
    private readonly IInvoicePdfLauncher invoicePdfLauncher;
    private EconomyOverview? currentOverview;
    private int year;
    private bool isBusy;
    private bool hasBlockingDataError;
    private string? errorMessage;
    private string manualEditStatusText = string.Empty;
    private string yearToDateNetText = string.Empty;
    private string yearToDateVatText = string.Empty;
    private string yearToDateTotalText = string.Empty;

    public EconomyOverviewViewModel(
        IEconomyOverviewService service,
        IEconomyOverviewExportDialog exportDialog,
        IEconomyOverviewCsvFileService csvFileService,
        IInvoicePdfLauncher invoicePdfLauncher,
        IUiClock clock,
        bool isAdminMode = false)
    {
        this.service = service;
        this.exportDialog = exportDialog;
        this.csvFileService = csvFileService;
        this.invoicePdfLauncher = invoicePdfLauncher;
        IsAdminMode = isAdminMode;
        year = clock.Now.Year;

        PreviousYearCommand = new AsyncRelayCommand(() => ChangeYearAsync(-1), () => !IsBusy);
        NextYearCommand = new AsyncRelayCommand(() => ChangeYearAsync(1), () => !IsBusy);
        ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync, CanUseCurrentOverview);
        SaveManualMonthsCommand = new AsyncRelayCommand(SaveManualMonthsAsync, () => IsAdminMode && CanEditCurrentOverview());
        CancelManualMonthsCommand = new AsyncRelayCommand(CancelManualMonthsAsync, () => IsAdminMode && CanEditCurrentOverview());
        OpenInvoicePdfCommand = new AsyncRelayCommand(
            parameter => OpenInvoicePdfAsync((EconomyOverviewInvoiceDisplayModel)parameter!),
            parameter => parameter is EconomyOverviewInvoiceDisplayModel && !IsBusy);
        DismissErrorCommand = new AsyncRelayCommand(() =>
        {
            ErrorMessage = null;
            return Task.CompletedTask;
        });
    }

    public ObservableCollection<EconomyOverviewInvoiceDisplayModel> Invoices { get; } = [];

    public ObservableCollection<EconomyOverviewMonthDisplayModel> Months { get; } = [];

    public ObservableCollection<EconomyOverviewPeriodTotalDisplayModel> Quarters { get; } = [];

    public ObservableCollection<EconomyOverviewPeriodTotalDisplayModel> Years { get; } = [];

    public int Year
    {
        get => year;
        private set
        {
            if (SetProperty(ref year, value))
            {
                OnPropertyChanged(nameof(YearText));
            }
        }
    }

    public string YearText => Year.ToString(CultureInfo.InvariantCulture);

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                RaiseCommandAvailabilityChanged();
            }
        }
    }

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

    public bool IsAdminMode { get; }

    public bool ShowAdminControls => IsAdminMode;

    public string ManualEditStatusText
    {
        get => manualEditStatusText;
        private set => SetProperty(ref manualEditStatusText, value);
    }

    public string YearToDateNetText
    {
        get => yearToDateNetText;
        private set => SetProperty(ref yearToDateNetText, value);
    }

    public string YearToDateVatText
    {
        get => yearToDateVatText;
        private set => SetProperty(ref yearToDateVatText, value);
    }

    public string YearToDateTotalText
    {
        get => yearToDateTotalText;
        private set => SetProperty(ref yearToDateTotalText, value);
    }

    public AsyncRelayCommand PreviousYearCommand { get; }

    public AsyncRelayCommand NextYearCommand { get; }

    public AsyncRelayCommand ExportCsvCommand { get; }

    public AsyncRelayCommand SaveManualMonthsCommand { get; }

    public AsyncRelayCommand CancelManualMonthsCommand { get; }

    public AsyncRelayCommand OpenInvoicePdfCommand { get; }

    public AsyncRelayCommand DismissErrorCommand { get; }

    public Task LoadAsync(CancellationToken cancellationToken = default) =>
        LoadYearAsync(Year, cancellationToken);

    private Task ChangeYearAsync(int delta) =>
        LoadYearAsync(Year + delta, CancellationToken.None);

    private async Task LoadYearAsync(int requestedYear, CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var result = await service.LoadAsync(requestedYear, cancellationToken);
            if (result.Value is not null)
            {
                ApplyOverview(result.Value);
            }

            if (!result.Succeeded || result.Value is null)
            {
                hasBlockingDataError = true;
                ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not load economy overview.";
                return;
            }

            hasBlockingDataError = false;
            ErrorMessage = null;
        }
        finally
        {
            IsBusy = false;
            RaiseCommandAvailabilityChanged();
        }
    }

    private async Task ExportCsvAsync()
    {
        if (currentOverview is null)
        {
            return;
        }

        var filePath = exportDialog.ShowSaveCsvDialog(Year);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await csvFileService.SaveAsync(currentOverview, filePath, CancellationToken.None);
            ErrorMessage = result.Succeeded
                ? null
                : result.Errors.FirstOrDefault()?.Message ?? "Could not export economy overview.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveManualMonthsAsync()
    {
        if (currentOverview is null)
        {
            return;
        }

        if (currentOverview.FirstApplicationInvoiceMonth is not { } firstApplicationInvoiceMonth)
        {
            ErrorMessage = "First application month must be set in Application Settings before saving manual month totals.";
            return;
        }

        var overrides = new List<EconomyManualMonthOverride>();
        foreach (var month in Months)
        {
            if (!month.CanEditManualTotals)
            {
                continue;
            }

            if (!TryCreateManualOverride(month, out var manualOverride, out var errorMessage))
            {
                ErrorMessage = errorMessage;
                return;
            }

            overrides.Add(manualOverride);
        }

        IsBusy = true;
        try
        {
            var result = await service.SaveManualMonthOverridesAsync(
                Year,
                new EconomySettings(firstApplicationInvoiceMonth),
                overrides,
                CancellationToken.None);
            if (result.Value is not null)
            {
                ApplyOverview(result.Value);
            }

            if (!result.Succeeded || result.Value is null)
            {
                hasBlockingDataError = true;
                ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not save manual month totals.";
                return;
            }

            hasBlockingDataError = false;
            ManualEditStatusText = "Manual month totals saved.";
            ErrorMessage = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task CancelManualMonthsAsync()
    {
        if (currentOverview is not null)
        {
            ApplyOverview(currentOverview);
            ManualEditStatusText = "Manual month edits discarded.";
        }

        return Task.CompletedTask;
    }

    private async Task OpenInvoicePdfAsync(EconomyOverviewInvoiceDisplayModel invoice)
    {
        try
        {
            await invoicePdfLauncher.OpenAsync(invoice.PdfFilePath, CancellationToken.None);
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void ApplyOverview(EconomyOverview overview)
    {
        currentOverview = overview;
        Year = overview.Year;
        Replace(
            Invoices,
            overview.Invoices.Select(invoice => new EconomyOverviewInvoiceDisplayModel(invoice)).ToArray());
        Replace(
            Months,
            overview.Months.Select(month => new EconomyOverviewPeriodTotalDisplayModel(
                new DateOnly(overview.Year, month.Month, 1).ToString("MMMM", DanishCulture),
                month.Net,
                month.Vat,
                month.Total)).Select((period, index) => new EconomyOverviewMonthDisplayModel(
                    overview.Year,
                    index + 1,
                    period.Label,
                    period.NetText,
                    period.VatText,
                    period.TotalText,
                    overview.Months[index].Source,
                    IsAdminMode,
                    overview.FirstApplicationInvoiceMonth)).ToArray());
        Replace(
            Quarters,
            overview.Quarters.Select(quarter => new EconomyOverviewPeriodTotalDisplayModel(
                $"Q{quarter.Quarter}",
                quarter.Net,
                quarter.Vat,
                quarter.Total)).ToArray());
        Replace(
            Years,
            overview.Years.Select(year => new EconomyOverviewPeriodTotalDisplayModel(
                year.Year.ToString(CultureInfo.InvariantCulture),
                year.Net,
                year.Vat,
                year.Total)).ToArray());
        YearToDateNetText = FormatMoney(overview.YearToDate.Net);
        YearToDateVatText = FormatMoney(overview.YearToDate.Vat);
        YearToDateTotalText = FormatMoney(overview.YearToDate.Total);
        ManualEditStatusText = IsAdminMode && overview.FirstApplicationInvoiceMonth is null
            ? "Set the first application month in Application Settings before editing manual month totals."
            : string.Empty;
    }

    private static void Replace<T>(ObservableCollection<T> target, IReadOnlyList<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    private void RaiseCommandAvailabilityChanged()
    {
        PreviousYearCommand.RaiseCanExecuteChanged();
        NextYearCommand.RaiseCanExecuteChanged();
        ExportCsvCommand.RaiseCanExecuteChanged();
        SaveManualMonthsCommand.RaiseCanExecuteChanged();
        CancelManualMonthsCommand.RaiseCanExecuteChanged();
        OpenInvoicePdfCommand.RaiseCanExecuteChanged();
    }

    private bool CanUseCurrentOverview() =>
        !IsBusy && currentOverview is not null && !hasBlockingDataError;

    private bool CanEditCurrentOverview() =>
        !IsBusy && currentOverview is not null;

    private static string FormatNullableMoney(Money? money) =>
        money is null ? string.Empty : FormatMoney(money.Value);

    private static string FormatMoney(Money money) =>
        string.Create(DanishCulture, $"{money.Amount:N2} {money.Currency}");

    private static string FormatEditableMoney(Money money) =>
        string.Create(DanishCulture, $"{money.Amount:N2} {money.Currency}");

    private static bool TryCreateManualOverride(
        EconomyOverviewMonthDisplayModel month,
        out EconomyManualMonthOverride manualOverride,
        out string errorMessage)
    {
        manualOverride = new EconomyManualMonthOverride(month.Year, month.Month, Money.Dkk(0m), Money.Dkk(0m));
        errorMessage = string.Empty;
        if (!TryParseEditableMoney(month.NetText, out var net))
        {
            errorMessage = $"Net amount for {month.Label} is not valid.";
            return false;
        }

        if (!TryParseEditableMoney(month.VatText, out var vat))
        {
            errorMessage = $"VAT amount for {month.Label} is not valid.";
            return false;
        }

        manualOverride = new EconomyManualMonthOverride(
            month.Year,
            month.Month,
            net ?? Money.Dkk(0m),
            vat ?? Money.Dkk(0m));
        return true;
    }

    private static bool TryParseEditableMoney(string? text, out Money? money)
    {
        money = null;
        var trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return true;
        }

        trimmed = trimmed.Replace("DKK", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        if (!decimal.TryParse(trimmed, NumberStyles.Number, DanishCulture, out var amount) &&
            !decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            return false;
        }

        if (amount < 0m)
        {
            return false;
        }

        money = Money.Dkk(amount);
        return true;
    }

    // Formats a saved invoice row while keeping its number and PDF path available to the overview.
    public sealed class EconomyOverviewInvoiceDisplayModel
    {
        public EconomyOverviewInvoiceDisplayModel(EconomyOverviewInvoiceRow row)
        {
            InvoiceNumber = row.InvoiceNumber;
            InvoiceNumberText = row.InvoiceNumber.ToString();
            PdfFilePath = row.PdfFilePath;
            PdfFileName = row.PdfFileName;
            NetText = FormatMoney(row.Net);
            VatText = FormatMoney(row.Vat);
            TotalText = FormatMoney(row.Total);
        }

        public InvoiceNumber InvoiceNumber { get; }

        public string InvoiceNumberText { get; }

        public string PdfFilePath { get; }

        public string PdfFileName { get; }

        public string NetText { get; }

        public string VatText { get; }

        public string TotalText { get; }
    }

    // Formats quarter or year totals for the read-only Economy grids.
    public sealed class EconomyOverviewPeriodTotalDisplayModel
    {
        public EconomyOverviewPeriodTotalDisplayModel(string label, Money? net, Money? vat, Money? total)
        {
            Label = label;
            NetText = FormatNullableMoney(net);
            VatText = FormatNullableMoney(vat);
            TotalText = FormatNullableMoney(total);
        }

        public string Label { get; }

        public string NetText { get; }

        public string VatText { get; }

        public string TotalText { get; }
    }

    // Tracks a month's displayed amounts and whether the current session can edit its manual values.
    public sealed class EconomyOverviewMonthDisplayModel : ViewModelBase
    {
        private string netText;
        private string vatText;
        private string totalText;
        private bool canEditManualTotals;

        public EconomyOverviewMonthDisplayModel(
            int year,
            int month,
            string label,
            string netText,
            string vatText,
            string totalText,
            EconomyOverviewMonthSource source,
            bool isAdminMode,
            DateOnly? firstApplicationInvoiceMonth)
        {
            Year = year;
            Month = month;
            Label = label;
            this.netText = ToEditableMoneyText(netText);
            this.vatText = ToEditableMoneyText(vatText);
            this.totalText = ToEditableMoneyText(totalText);
            Source = source;
            SetAdminEditability(isAdminMode, firstApplicationInvoiceMonth);
        }

        public int Year { get; }

        public int Month { get; }

        public string Label { get; }

        public EconomyOverviewMonthSource Source { get; }

        public string NetText
        {
            get => netText;
            set
            {
                if (SetProperty(ref netText, value))
                {
                    RecalculateTotalText();
                }
            }
        }

        public string VatText
        {
            get => vatText;
            set
            {
                if (SetProperty(ref vatText, value))
                {
                    RecalculateTotalText();
                }
            }
        }

        public string TotalText
        {
            get => totalText;
            private set => SetProperty(ref totalText, value);
        }

        public bool CanEditManualTotals
        {
            get => canEditManualTotals;
            private set => SetProperty(ref canEditManualTotals, value);
        }

        public void SetAdminEditability(bool isAdminMode, DateOnly? firstApplicationInvoiceMonth)
        {
            var isLegacyManualMonth = firstApplicationInvoiceMonth is not null &&
                new DateOnly(Year, Month, 1) < firstApplicationInvoiceMonth.Value;
            CanEditManualTotals = isAdminMode &&
                isLegacyManualMonth &&
                Source != EconomyOverviewMonthSource.IssuedInvoices;
        }

        private void RecalculateTotalText()
        {
            if (!TryParseEditableMoney(NetText, out var net) ||
                !TryParseEditableMoney(VatText, out var vat))
            {
                TotalText = string.Empty;
                return;
            }

            if (net is null && vat is null)
            {
                TotalText = string.Empty;
                return;
            }

            TotalText = FormatEditableMoney((net ?? Money.Dkk(0m)) + (vat ?? Money.Dkk(0m)));
        }

        private static string ToEditableMoneyText(string value)
        {
            if (!TryParseEditableMoney(value, out var money) || money is null)
            {
                return string.Empty;
            }

            return FormatEditableMoney(money.Value);
        }
    }
}
