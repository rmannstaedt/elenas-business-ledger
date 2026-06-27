using System.Collections.ObjectModel;
using System.Globalization;
using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Money;
using MySmallBusinessLedger.Core.UseCases.Economy;

namespace MySmallBusinessLedger.App.ViewModels;

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
    private string? errorMessage;
    private string yearToDateNetText = string.Empty;
    private string yearToDateVatText = string.Empty;
    private string yearToDateTotalText = string.Empty;

    public EconomyOverviewViewModel(
        IEconomyOverviewService service,
        IEconomyOverviewExportDialog exportDialog,
        IEconomyOverviewCsvFileService csvFileService,
        IInvoicePdfLauncher invoicePdfLauncher,
        IUiClock clock)
    {
        this.service = service;
        this.exportDialog = exportDialog;
        this.csvFileService = csvFileService;
        this.invoicePdfLauncher = invoicePdfLauncher;
        year = clock.Now.Year;

        PreviousYearCommand = new AsyncRelayCommand(() => ChangeYearAsync(-1), () => !IsBusy);
        NextYearCommand = new AsyncRelayCommand(() => ChangeYearAsync(1), () => !IsBusy);
        ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync, () => !IsBusy && currentOverview is not null);
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

    public ObservableCollection<EconomyOverviewPeriodTotalDisplayModel> Months { get; } = [];

    public ObservableCollection<EconomyOverviewPeriodTotalDisplayModel> Quarters { get; } = [];

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
            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not load economy overview.";
                return;
            }

            ApplyOverview(result.Value);
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
                month.Total)).ToArray());
        Replace(
            Quarters,
            overview.Quarters.Select(quarter => new EconomyOverviewPeriodTotalDisplayModel(
                $"Q{quarter.Quarter}",
                quarter.Net,
                quarter.Vat,
                quarter.Total)).ToArray());
        YearToDateNetText = FormatMoney(overview.YearToDate.Net);
        YearToDateVatText = FormatMoney(overview.YearToDate.Vat);
        YearToDateTotalText = FormatMoney(overview.YearToDate.Total);
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
        OpenInvoicePdfCommand.RaiseCanExecuteChanged();
    }

    private static string FormatNullableMoney(Money? money) =>
        money is null ? string.Empty : FormatMoney(money.Value);

    private static string FormatMoney(Money money) =>
        string.Create(DanishCulture, $"{money.Amount:N2} {money.Currency}");

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
}
