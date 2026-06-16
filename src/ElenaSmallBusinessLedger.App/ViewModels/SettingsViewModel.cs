using ElenaSmallBusinessLedger.App.Commands;
using ElenaSmallBusinessLedger.App.Services;
using ElenaSmallBusinessLedger.Core.Company;
using ElenaSmallBusinessLedger.Infrastructure.Persistence;

namespace ElenaSmallBusinessLedger.App.ViewModels;

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
    private string googleClientSecretsSourcePath = string.Empty;
    private bool hasGoogleClientSecrets;
    private string? errorMessage;

    public SettingsViewModel(IAppSettingsService settingsService)
    {
        this.settingsService = settingsService;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
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

    public string GoogleClientSecretsSourcePath
    {
        get => googleClientSecretsSourcePath;
        set => SetProperty(ref googleClientSecretsSourcePath, value);
    }

    public bool HasGoogleClientSecrets
    {
        get => hasGoogleClientSecrets;
        private set => SetProperty(ref hasGoogleClientSecrets, value);
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

        HasGoogleClientSecrets = snapshot.HasGoogleClientSecrets;
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
            string.IsNullOrWhiteSpace(GoogleClientSecretsSourcePath) ? null : GoogleClientSecretsSourcePath,
            CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not save settings.";
            return;
        }

        HasGoogleClientSecrets = result.Value!.HasGoogleClientSecrets;
        WasSaved = true;
        ErrorMessage = null;
        CloseRequested?.Invoke(this, true);
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
}
