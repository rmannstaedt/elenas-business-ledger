using System.Collections.ObjectModel;
using ElenasBusinessLedger.App.Commands;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Company;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Calendar;

namespace ElenasBusinessLedger.App.ViewModels;

// Holds editable preferences and connection status so the user can save Settings explicitly.
public sealed class SettingsViewModel : ViewModelBase
{
    private const string DefaultCalendarId = "primary";
    private readonly IAppSettingsService settingsService;
    private readonly IGoogleCalendarAuthorizationService calendarAuthorization;
    private string companyName = string.Empty;
    private string companyAddress = string.Empty;
    private string companyAddressLine2 = string.Empty;
    private string companyPhone = string.Empty;
    private string companyEmail = string.Empty;
    private string companyCvr = string.Empty;
    private bool isVatRegistered;
    private DateTime? vatRegistrationDate;
    private string invoiceFolderPath = string.Empty;
    private string configuredInvoicePrinterName = string.Empty;
    private PaymentInstructionChoice paymentInstructionChoice = PaymentInstructionChoice.DueOnReceipt;
    private string customPaymentInstructions = string.Empty;
    private string bankRegistrationNumber = string.Empty;
    private string bankAccountNumber = string.Empty;
    private string mobilePayAccount = string.Empty;
    private string calendarId = DefaultCalendarId;
    private string googleAccountEmail = string.Empty;
    private GoogleOAuthClientConfigurationStatus googleOAuthAppConfigurationStatus = GoogleOAuthClientConfigurationStatus.Missing;
    private string googleAuthorizationStatusText = "Google authorization: Not connected";
    private string appDataFolderPath = string.Empty;
    private string databasePath = string.Empty;
    private bool isAdminMode;
    private DateTime? firstApplicationInvoiceMonthDate;
    private string? errorMessage;

    public SettingsViewModel(
        IAppSettingsService settingsService,
        IGoogleCalendarAuthorizationService calendarAuthorization,
        bool isAdminMode = false)
    {
        this.settingsService = settingsService;
        this.calendarAuthorization = calendarAuthorization;
        this.isAdminMode = isAdminMode;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ClearInvoicePrinterCommand = new AsyncRelayCommand(ClearInvoicePrinterAsync);
        ConnectGoogleCalendarCommand = new AsyncRelayCommand(ConnectGoogleCalendarAsync);
        ForgetGoogleAuthorizationCommand = new AsyncRelayCommand(ForgetGoogleAuthorizationAsync);
    }

    public string CompanyName { get => companyName; set => SetProperty(ref companyName, value); }

    public string CompanyAddress { get => companyAddress; set => SetProperty(ref companyAddress, value); }

    public string CompanyAddressLine2 { get => companyAddressLine2; set => SetProperty(ref companyAddressLine2, value); }

    public string CompanyPhone { get => companyPhone; set => SetProperty(ref companyPhone, value); }

    public string CompanyEmail { get => companyEmail; set => SetProperty(ref companyEmail, value); }

    public string CompanyCvr { get => companyCvr; set => SetProperty(ref companyCvr, value); }

    public bool IsVatRegistered { get => isVatRegistered; set => SetProperty(ref isVatRegistered, value); }

    public DateTime? VatRegistrationDate { get => vatRegistrationDate; set => SetProperty(ref vatRegistrationDate, value); }

    public string InvoiceFolderPath { get => invoiceFolderPath; set => SetProperty(ref invoiceFolderPath, value); }

    public string ConfiguredInvoicePrinterName
    {
        get => configuredInvoicePrinterName;
        set => SetProperty(ref configuredInvoicePrinterName, value);
    }

    public string PaymentInstructions
    {
        get => CreatePaymentInstructions();
        set => ApplyPaymentInstructions(value);
    }

    public bool IsPaymentDueOnReceipt
    {
        get => paymentInstructionChoice == PaymentInstructionChoice.DueOnReceipt;
        set => SetPaymentInstructionChoice(PaymentInstructionChoice.DueOnReceipt, value);
    }

    public bool IsPaymentDueIn8Days
    {
        get => paymentInstructionChoice == PaymentInstructionChoice.DueIn8Days;
        set => SetPaymentInstructionChoice(PaymentInstructionChoice.DueIn8Days, value);
    }

    public bool IsPaymentDueIn30Days
    {
        get => paymentInstructionChoice == PaymentInstructionChoice.DueIn30Days;
        set => SetPaymentInstructionChoice(PaymentInstructionChoice.DueIn30Days, value);
    }

    public bool IsCustomPaymentInstructions
    {
        get => paymentInstructionChoice == PaymentInstructionChoice.Custom;
        set => SetPaymentInstructionChoice(PaymentInstructionChoice.Custom, value);
    }

    public string CustomPaymentInstructions
    {
        get => customPaymentInstructions;
        set
        {
            if (SetProperty(ref customPaymentInstructions, value) && IsCustomPaymentInstructions)
            {
                OnPropertyChanged(nameof(PaymentInstructions));
            }
        }
    }

    public string BankRegistrationNumber
    {
        get => bankRegistrationNumber;
        set => SetProperty(ref bankRegistrationNumber, value);
    }

    public string BankAccountNumber
    {
        get => bankAccountNumber;
        set => SetProperty(ref bankAccountNumber, value);
    }

    public string MobilePayAccount { get => mobilePayAccount; set => SetProperty(ref mobilePayAccount, value); }

    public ObservableCollection<string> AvailableInvoicePrinters { get; } = [];

    public string CalendarId { get => calendarId; set => SetProperty(ref calendarId, value); }

    public string CalendarIdHelpText =>
        "Use primary for the main calendar of the Google account you sign in with. "
        + "For a separate calendar, open Google Calendar in a browser, choose the calendar's Settings and sharing, "
        + "then copy Calendar ID from Integrate calendar.";

    public string GoogleAccountEmail { get => googleAccountEmail; set => SetProperty(ref googleAccountEmail, value); }

    public bool HasGoogleOAuthAppConfiguration => googleOAuthAppConfigurationStatus == GoogleOAuthClientConfigurationStatus.Ready;

    public string GoogleOAuthAppStatusText =>
        googleOAuthAppConfigurationStatus switch
        {
            GoogleOAuthClientConfigurationStatus.Ready => "Google sign-in app: Ready",
            GoogleOAuthClientConfigurationStatus.Invalid => "Google sign-in app: Invalid",
            _ => "Google sign-in app: Missing"
        };

    public string GoogleAuthorizationStatusText
    {
        get => googleAuthorizationStatusText;
        private set => SetProperty(ref googleAuthorizationStatusText, value);
    }

    public string AppDataFolderPath
    {
        get => appDataFolderPath;
        private set => SetProperty(ref appDataFolderPath, value);
    }

    public string DatabasePath
    {
        get => databasePath;
        private set => SetProperty(ref databasePath, value);
    }

    public bool IsAdminMode
    {
        get => isAdminMode;
        set
        {
            if (SetProperty(ref isAdminMode, value))
            {
                OnPropertyChanged(nameof(ShowEconomyAdministration));
            }
        }
    }

    public bool ShowEconomyAdministration => IsAdminMode;

    public DateTime? FirstApplicationInvoiceMonthDate
    {
        get => firstApplicationInvoiceMonthDate;
        set => SetProperty(ref firstApplicationInvoiceMonthDate, NormalizeMonthDate(value));
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

    public bool WasSaved { get; private set; }

    public bool SavedAdminMode { get; private set; }

    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand ClearInvoicePrinterCommand { get; }

    public AsyncRelayCommand ConnectGoogleCalendarCommand { get; }

    public AsyncRelayCommand ForgetGoogleAuthorizationCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    // Fills the settings form from saved values and shows the current printer and Google authorization state.
    public async Task LoadAsync()
    {
        var snapshot = await settingsService.LoadAsync(CancellationToken.None);
        if (snapshot.CompanyProfile is { } company)
        {
            var addressLines = SplitCompanyAddress(company.Address);
            CompanyName = company.Name;
            CompanyAddress = addressLines.Line1;
            CompanyAddressLine2 = addressLines.Line2;
            CompanyPhone = company.Phone;
            CompanyEmail = company.Email;
            CompanyCvr = company.Cvr ?? string.Empty;
            IsVatRegistered = company.VatStatus == VatStatus.Registered;
            VatRegistrationDate = company.VatRegistrationDate?.ToDateTime(TimeOnly.MinValue);
            InvoiceFolderPath = company.InvoiceFolderPath;
            ConfiguredInvoicePrinterName = company.ConfiguredInvoicePrinterName;
            ApplyPaymentInstructions(company.PaymentInstructions);
            BankRegistrationNumber = company.BankRegistrationNumber;
            BankAccountNumber = company.BankAccountNumber;
            MobilePayAccount = company.MobilePayAccount;
        }

        AvailableInvoicePrinters.Clear();
        foreach (var printerName in snapshot.AvailablePrinterNames ?? [])
        {
            AvailableInvoicePrinters.Add(printerName);
        }

        if (snapshot.CalendarSettings is { } calendar)
        {
            CalendarId = NormalizeCalendarId(calendar.SelectedCalendarId);
            GoogleAccountEmail = calendar.GoogleAccountEmail;
        }

        SetGoogleOAuthAppConfigurationStatus(snapshot.GoogleOAuthAppConfigurationStatus);
        GoogleAuthorizationStatusText = snapshot.HasGoogleAuthorizationTokens
            ? "Google authorization: Saved locally." + Environment.NewLine + "Connect again if Google Calendar fails."
            : "Google authorization: Not connected";
        AppDataFolderPath = snapshot.AppDataFolderPath;
        DatabasePath = snapshot.DatabasePath;
        FirstApplicationInvoiceMonthDate = snapshot.EconomySettings?.FirstApplicationInvoiceMonth.ToDateTime(TimeOnly.MinValue);
    }

    private async Task SaveAsync()
    {
        if (!Validate())
        {
            return;
        }

        var profile = CompanyProfile.Create(
            CompanyName.Trim(),
            CombineCompanyAddress(CompanyAddress, CompanyAddressLine2),
            CompanyPhone.Trim(),
            CompanyEmail.Trim(),
            string.IsNullOrWhiteSpace(CompanyCvr) ? null : CompanyCvr.Trim(),
            IsVatRegistered && VatRegistrationDate is not null ? DateOnly.FromDateTime(VatRegistrationDate.Value) : null,
            IsVatRegistered ? VatStatus.Registered : VatStatus.NotRegistered,
            InvoiceFolderPath.Trim(),
            ConfiguredInvoicePrinterName.Trim(),
            PaymentInstructions.Trim(),
            BankRegistrationNumber.Trim(),
            BankAccountNumber.Trim(),
            MobilePayAccount.Trim());
        CalendarId = NormalizeCalendarId(CalendarId);
        var calendarSettings = new CalendarSettings(CalendarId, GoogleAccountEmail.Trim());
        var economySettings = IsAdminMode && FirstApplicationInvoiceMonthDate is not null
            ? new EconomySettings(NormalizeMonth(DateOnly.FromDateTime(FirstApplicationInvoiceMonthDate.Value)))
            : null;

        var result = await settingsService.SaveAsync(
            profile,
            calendarSettings,
            economySettings,
            CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not save settings.";
            return;
        }

        SetGoogleOAuthAppConfigurationStatus(result.Value!.GoogleOAuthAppConfigurationStatus);
        WasSaved = true;
        SavedAdminMode = IsAdminMode;
        ErrorMessage = null;
        CloseRequested?.Invoke(this, true);
    }

    private Task ClearInvoicePrinterAsync()
    {
        ConfiguredInvoicePrinterName = string.Empty;
        return Task.CompletedTask;
    }

    private async Task ConnectGoogleCalendarAsync()
    {
        CalendarId = NormalizeCalendarId(CalendarId);

        if (string.IsNullOrWhiteSpace(GoogleAccountEmail))
        {
            ErrorMessage = "Google account email is required before connecting Google Calendar.";
            return;
        }

        var result = await calendarAuthorization.ConnectGoogleCalendarAsync(
            new CalendarSettings(CalendarId, GoogleAccountEmail.Trim()),
            CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Google Calendar connection failed.";
            GoogleAuthorizationStatusText = ErrorMessage;
            return;
        }

        ErrorMessage = null;
        GoogleAuthorizationStatusText = "Google Calendar connection succeeded. Save settings to use this calendar.";
    }

    private async Task ForgetGoogleAuthorizationAsync()
    {
        var result = await calendarAuthorization.ForgetGoogleAuthorizationAsync(CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Google authorization could not be cleared.";
            GoogleAuthorizationStatusText = ErrorMessage;
            return;
        }

        ErrorMessage = null;
        GoogleAuthorizationStatusText = "Google authorization cleared. The next connection will ask you to sign in.";
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            ErrorMessage = "Company name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(InvoiceFolderPath))
        {
            ErrorMessage = "Invoice folder is required.";
            return false;
        }

        if (IsVatRegistered && VatRegistrationDate is null)
        {
            ErrorMessage = "VAT registration date is required when VAT is registered.";
            return false;
        }

        if (IsAdminMode && FirstApplicationInvoiceMonthDate is null)
        {
            ErrorMessage = "First application month is required when Admin Mode is enabled.";
            return false;
        }

        ErrorMessage = null;
        return true;
    }

    private void SetGoogleOAuthAppConfigurationStatus(GoogleOAuthClientConfigurationStatus status)
    {
        if (SetProperty(ref googleOAuthAppConfigurationStatus, status, nameof(HasGoogleOAuthAppConfiguration)))
        {
            OnPropertyChanged(nameof(GoogleOAuthAppStatusText));
        }
    }

    private static string NormalizeCalendarId(string value) =>
        string.IsNullOrWhiteSpace(value) ? DefaultCalendarId : value.Trim();

    private static DateOnly NormalizeMonth(DateOnly month) =>
        new(month.Year, month.Month, 1);

    private static DateTime? NormalizeMonthDate(DateTime? date) =>
        date is null ? null : new DateTime(date.Value.Year, date.Value.Month, 1);

    private string CreatePaymentInstructions() =>
        paymentInstructionChoice switch
        {
            PaymentInstructionChoice.DueIn8Days => "8 dage fra fakturadato",
            PaymentInstructionChoice.DueIn30Days => "30 dage fra fakturadato",
            PaymentInstructionChoice.Custom => CustomPaymentInstructions.Trim(),
            _ => "Betales ved modtagelse"
        };

    private void ApplyPaymentInstructions(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        switch (normalized)
        {
            case "":
            case "Betales ved modtagelse":
            case "Betales ved modtagelse.":
                SetPaymentInstructionChoice(PaymentInstructionChoice.DueOnReceipt, true);
                break;
            case "8 dage fra fakturadato":
                SetPaymentInstructionChoice(PaymentInstructionChoice.DueIn8Days, true);
                break;
            case "30 dage fra fakturadato":
                SetPaymentInstructionChoice(PaymentInstructionChoice.DueIn30Days, true);
                break;
            default:
                CustomPaymentInstructions = normalized;
                SetPaymentInstructionChoice(PaymentInstructionChoice.Custom, true);
                break;
        }
    }

    private void SetPaymentInstructionChoice(PaymentInstructionChoice choice, bool isSelected)
    {
        if (!isSelected || paymentInstructionChoice == choice)
        {
            return;
        }

        paymentInstructionChoice = choice;
        OnPropertyChanged(nameof(IsPaymentDueOnReceipt));
        OnPropertyChanged(nameof(IsPaymentDueIn8Days));
        OnPropertyChanged(nameof(IsPaymentDueIn30Days));
        OnPropertyChanged(nameof(IsCustomPaymentInstructions));
        OnPropertyChanged(nameof(PaymentInstructions));
    }

    private static (string Line1, string Line2) SplitCompanyAddress(string address)
    {
        var normalized = address.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var separatorIndex = normalized.IndexOf('\n', StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return (address, string.Empty);
        }

        var line1 = normalized[..separatorIndex];
        var line2 = normalized[(separatorIndex + 1)..].Replace("\n", Environment.NewLine, StringComparison.Ordinal);
        return (line1, line2);
    }

    private static string CombineCompanyAddress(string line1, string line2)
    {
        var trimmedLine1 = line1.Trim();
        var trimmedLine2 = line2.Trim();
        return string.IsNullOrWhiteSpace(trimmedLine2)
            ? trimmedLine1
            : trimmedLine1 + Environment.NewLine + trimmedLine2;
    }

    private enum PaymentInstructionChoice
    {
        DueOnReceipt,
        DueIn8Days,
        DueIn30Days,
        Custom
    }
}
