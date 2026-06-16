using ElenaSmallBusinessLedger.App.Services;
using ElenaSmallBusinessLedger.App.ViewModels;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Company;
using ElenaSmallBusinessLedger.Infrastructure.Persistence;

namespace ElenaSmallBusinessLedger.Tests.App.ViewModels;

public sealed class SettingsViewModelTests
{
    [Fact]
    public async Task LoadsExistingSettings()
    {
        var service = new FakeAppSettingsService
        {
            Snapshot = new AppSettingsSnapshot(
                CompanyProfile.Create(
                    "Maple Key Studio",
                    "Musikvej 1",
                    "12345678",
                    "elena@example.test",
                    "12345678",
                    new DateOnly(2026, 7, 1),
                    VatStatus.Registered,
                    @"C:\Invoices"),
                new CalendarSettings("primary", "studio@example.test"),
                HasGoogleClientSecrets: true)
        };
        var viewModel = new SettingsViewModel(service);

        await viewModel.LoadAsync();

        Assert.Equal("Maple Key Studio", viewModel.CompanyName);
        Assert.Equal("primary", viewModel.CalendarId);
        Assert.True(viewModel.IsVatRegistered);
        Assert.True(viewModel.HasGoogleClientSecrets);
    }

    [Fact]
    public async Task SavePersistsCompanyAndCalendarSettings()
    {
        var service = new FakeAppSettingsService();
        var viewModel = new SettingsViewModel(service)
        {
            CompanyName = "Maple Key Studio",
            CompanyAddress = "Musikvej 1",
            CompanyPhone = "12345678",
            CompanyEmail = "elena@example.test",
            CompanyCvr = "12345678",
            IsVatRegistered = true,
            VatRegistrationDate = new DateTime(2026, 7, 1),
            InvoiceFolderPath = @"C:\Invoices",
            CalendarId = "primary",
            GoogleAccountEmail = "studio@example.test"
        };

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.NotNull(service.SavedProfile);
        Assert.Equal(VatStatus.Registered, service.SavedProfile.VatStatus);
        Assert.Equal(new DateOnly(2026, 7, 1), service.SavedProfile.VatRegistrationDate);
        Assert.Equal(new CalendarSettings("primary", "studio@example.test"), service.SavedCalendarSettings);
        Assert.True(viewModel.WasSaved);
    }

    [Fact]
    public async Task SaveRequiresCompanyNameAndInvoiceFolder()
    {
        var service = new FakeAppSettingsService();
        var viewModel = new SettingsViewModel(service)
        {
            CompanyName = "",
            InvoiceFolderPath = ""
        };

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.Null(service.SavedProfile);
        Assert.Contains("Company name", viewModel.ErrorMessage);
    }

    private sealed class FakeAppSettingsService : IAppSettingsService
    {
        public AppSettingsSnapshot Snapshot { get; init; } = new(null, null, HasGoogleClientSecrets: false);

        public CompanyProfile? SavedProfile { get; private set; }

        public CalendarSettings? SavedCalendarSettings { get; private set; }

        public Task<AppSettingsSnapshot> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Snapshot);

        public Task<OperationResult<AppSettingsSnapshot>> SaveAsync(
            CompanyProfile companyProfile,
            CalendarSettings calendarSettings,
            string? googleClientSecretsSourcePath,
            CancellationToken cancellationToken)
        {
            SavedProfile = companyProfile;
            SavedCalendarSettings = calendarSettings;
            return Task.FromResult(OperationResult<AppSettingsSnapshot>.Success(
                new AppSettingsSnapshot(companyProfile, calendarSettings, HasGoogleClientSecrets: true)));
        }
    }
}
