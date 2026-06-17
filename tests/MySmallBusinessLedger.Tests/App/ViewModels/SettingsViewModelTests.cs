using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.Tests.App.ViewModels;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void SettingsViewInitializesWithReadOnlyPathBindings()
    {
        RunOnStaThread(() =>
        {
            var viewModel = new SettingsViewModel(new FakeAppSettingsService());
            var window = new SettingsView(viewModel);

            Assert.Equal("Application Settings", window.Title);
            Assert.Same(viewModel, window.DataContext);
            Assert.NotNull(window.Content);

            window.Close();
        });
    }

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
                HasGoogleClientSecrets: true,
                AppDataFolderPath: @"C:\Users\ExampleUser\AppData\Roaming\visualtech.dk\MySmallBusinessLedger",
                DatabasePath: @"C:\Users\ExampleUser\AppData\Roaming\visualtech.dk\MySmallBusinessLedger\ledger.db")
        };
        var viewModel = new SettingsViewModel(service);

        await viewModel.LoadAsync();

        Assert.Equal("Maple Key Studio", viewModel.CompanyName);
        Assert.Equal("primary", viewModel.CalendarId);
        Assert.True(viewModel.IsVatRegistered);
        Assert.True(viewModel.HasGoogleClientSecrets);
        Assert.Equal("Google login setup file saved: Yes", viewModel.GoogleLoginSetupFileStatusText);
        Assert.Equal(@"C:\Users\ExampleUser\AppData\Roaming\visualtech.dk\MySmallBusinessLedger", viewModel.AppDataFolderPath);
        Assert.Equal(@"C:\Users\ExampleUser\AppData\Roaming\visualtech.dk\MySmallBusinessLedger\ledger.db", viewModel.DatabasePath);
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

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw exception;
        }
    }
}
