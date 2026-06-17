using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsService settingsService;
    private string companyName = string.Empty;
    private string companyAddress = string.Empty;
    private string companyPhone = string.Empty;
    private string companyEmail = string.Empty;
    private string companyCvr = string.Empty;
    private bool isVatRegistered;
    private DateTime? vatRegistrationDate;
    private string invoiceFolderPath = string.Empty;
    private string calendarId = string.Empty;
    private string googleAccountEmail = string.Empty;
    private GoogleOAuthClientConfigurationStatus googleOAuthAppConfigurationStatus = GoogleOAuthClientConfigurationStatus.Missing;
    private string googleAuthorizationStatusText = "Google authorization: Not checked";
    private string appDataFolderPath = string.Empty;
    private string databasePath = string.Empty;
    private string? errorMessage;

    public SettingsViewModel(IAppSettingsService settingsService)
    {
        this.settingsService = settingsService;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ConnectGoogleCalendarCommand = new AsyncRelayCommand(ConnectGoogleCalendarAsync);
        ForgetGoogleAuthorizationCommand = new AsyncRelayCommand(ForgetGoogleAuthorizationAsync);
    }

    public string CompanyName { get => companyName; set => SetProperty(ref companyName, value); }

    public string CompanyAddress { get => companyAddress; set => SetProperty(ref companyAddress, value); }

    public string CompanyPhone { get => companyPhone; set => SetProperty(ref companyPhone, value); }

    public string CompanyEmail { get => companyEmail; set => SetProperty(ref companyEmail, value); }

    public string CompanyCvr { get => companyCvr; set => SetProperty(ref companyCvr, value); }

    public bool IsVatRegistered { get => isVatRegistered; set => SetProperty(ref isVatRegistered, value); }

    public DateTime? VatRegistrationDate { get => vatRegistrationDate; set => SetProperty(ref vatRegistrationDate, value); }

    public string InvoiceFolderPath { get => invoiceFolderPath; set => SetProperty(ref invoiceFolderPath, value); }

    public string CalendarId { get => calendarId; set => SetProperty(ref calendarId, value); }

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

    public AsyncRelayCommand ConnectGoogleCalendarCommand { get; }

    public AsyncRelayCommand ForgetGoogleAuthorizationCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public async Task LoadAsync()
    {
        var snapshot = await settingsService.LoadAsync(CancellationToken.None);
        if (snapshot.CompanyProfile is { } company)
        {
            CompanyName = company.Name;
            CompanyAddress = company.Address;
            CompanyPhone = company.Phone;
            CompanyEmail = company.Email;
            CompanyCvr = company.Cvr ?? string.Empty;
            IsVatRegistered = company.VatStatus == VatStatus.Registered;
            VatRegistrationDate = company.VatRegistrationDate?.ToDateTime(TimeOnly.MinValue);
            InvoiceFolderPath = company.InvoiceFolderPath;
        }

        if (snapshot.CalendarSettings is { } calendar)
        {
            CalendarId = calendar.SelectedCalendarId;
            GoogleAccountEmail = calendar.GoogleAccountEmail;
        }

        SetGoogleOAuthAppConfigurationStatus(snapshot.GoogleOAuthAppConfigurationStatus);
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
            CompanyAddress.Trim(),
            CompanyPhone.Trim(),
            CompanyEmail.Trim(),
            string.IsNullOrWhiteSpace(CompanyCvr) ? null : CompanyCvr.Trim(),
            IsVatRegistered && VatRegistrationDate is not null ? DateOnly.FromDateTime(VatRegistrationDate.Value) : null,
            IsVatRegistered ? VatStatus.Registered : VatStatus.NotRegistered,
            InvoiceFolderPath.Trim());
        var calendarSettings = new CalendarSettings(CalendarId.Trim(), GoogleAccountEmail.Trim());

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

    private async Task ConnectGoogleCalendarAsync()
    {
        if (string.IsNullOrWhiteSpace(CalendarId))
        {
            ErrorMessage = "Calendar ID is required before connecting Google Calendar.";
            return;
        }

        if (string.IsNullOrWhiteSpace(GoogleAccountEmail))
        {
            ErrorMessage = "Google account email is required before connecting Google Calendar.";
            return;
        }

        var result = await settingsService.ConnectGoogleCalendarAsync(
            new CalendarSettings(CalendarId.Trim(), GoogleAccountEmail.Trim()),
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
}
