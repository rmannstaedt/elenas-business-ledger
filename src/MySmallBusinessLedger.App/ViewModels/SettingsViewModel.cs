using System.Collections.ObjectModel;
using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private const string DefaultCalendarId = "primary";
    private readonly IAppSettingsService settingsService;
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
    private string calendarId = DefaultCalendarId;
    private string googleAccountEmail = string.Empty;
    private GoogleOAuthClientConfigurationStatus googleOAuthAppConfigurationStatus = GoogleOAuthClientConfigurationStatus.Missing;
    private string googleAuthorizationStatusText = "Google authorization: Not connected";
    private string appDataFolderPath = string.Empty;
    private string databasePath = string.Empty;
    private string? errorMessage;

    public SettingsViewModel(IAppSettingsService settingsService)
    {
        this.settingsService = settingsService;
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

    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand ClearInvoicePrinterCommand { get; }

    public AsyncRelayCommand ConnectGoogleCalendarCommand { get; }

    public AsyncRelayCommand ForgetGoogleAuthorizationCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

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
            ? "Google authorization: Saved locally. Connect again if Google Calendar access fails."
            : "Google authorization: Not connected";
        AppDataFolderPath = snapshot.AppDataFolderPath;
        DatabasePath = snapshot.DatabasePath;
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
            ConfiguredInvoicePrinterName.Trim());
        CalendarId = NormalizeCalendarId(CalendarId);
        var calendarSettings = new CalendarSettings(CalendarId, GoogleAccountEmail.Trim());

        var result = await settingsService.SaveAsync(
            profile,
            calendarSettings,
            CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not save settings.";
            return;
        }

        SetGoogleOAuthAppConfigurationStatus(result.Value!.GoogleOAuthAppConfigurationStatus);
        WasSaved = true;
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

        var result = await settingsService.ConnectGoogleCalendarAsync(
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
        var result = await settingsService.ForgetGoogleAuthorizationAsync(CancellationToken.None);
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
}
