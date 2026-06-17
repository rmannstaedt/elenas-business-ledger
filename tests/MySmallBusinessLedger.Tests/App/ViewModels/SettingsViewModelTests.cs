using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Infrastructure.Persistence;
using System.Windows;
using System.Windows.Controls;

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
            Assert.NotNull(FindButtonByContent(window, "Browse..."));

            window.Close();
        });
    }

    [Fact]
    public void DefaultsCalendarIdToPrimaryAndExplainsWhereToFindAnotherCalendar()
    {
        var viewModel = new SettingsViewModel(new FakeAppSettingsService());

        Assert.Equal("primary", viewModel.CalendarId);
        Assert.Contains("primary", viewModel.CalendarIdHelpText);
        Assert.Contains("Settings and sharing", viewModel.CalendarIdHelpText);
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
                GoogleOAuthAppConfigurationStatus: GoogleOAuthClientConfigurationStatus.Ready,
                AppDataFolderPath: @"C:\Users\ExampleUser\AppData\Roaming\visualtech.dk\MySmallBusinessLedger",
                DatabasePath: @"C:\Users\ExampleUser\AppData\Roaming\visualtech.dk\MySmallBusinessLedger\ledger.db")
        };
        var viewModel = new SettingsViewModel(service);

        await viewModel.LoadAsync();

        Assert.Equal("Maple Key Studio", viewModel.CompanyName);
        Assert.Equal("primary", viewModel.CalendarId);
        Assert.True(viewModel.IsVatRegistered);
        Assert.True(viewModel.HasGoogleOAuthAppConfiguration);
        Assert.Equal("Google sign-in app: Ready", viewModel.GoogleOAuthAppStatusText);
        Assert.Equal(@"C:\Users\ExampleUser\AppData\Roaming\visualtech.dk\MySmallBusinessLedger", viewModel.AppDataFolderPath);
        Assert.Equal(@"C:\Users\ExampleUser\AppData\Roaming\visualtech.dk\MySmallBusinessLedger\ledger.db", viewModel.DatabasePath);
        Assert.Equal("Google authorization: Not connected", viewModel.GoogleAuthorizationStatusText);
    }

    [Fact]
    public async Task LoadSplitsSavedMultilineCompanyAddressAcrossAddressFields()
    {
        var service = new FakeAppSettingsService
        {
            Snapshot = new AppSettingsSnapshot(
                CompanyProfile.Create(
                    "Maple Key Studio",
                    "18 Example Lane" + Environment.NewLine + "0000 Example City",
                    "12345678",
                    "elena@example.test",
                    null,
                    null,
                    VatStatus.NotRegistered,
                    @"C:\Invoices"),
                null,
                GoogleOAuthClientConfigurationStatus.Ready)
        };
        var viewModel = new SettingsViewModel(service);

        await viewModel.LoadAsync();

        Assert.Equal("18 Example Lane", viewModel.CompanyAddress);
        Assert.Equal("0000 Example City", viewModel.CompanyAddressLine2);
    }

    [Fact]
    public async Task SaveCombinesCompanyAddressLines()
    {
        var service = new FakeAppSettingsService();
        var viewModel = new SettingsViewModel(service)
        {
            CompanyName = "Maple Key Studio",
            CompanyAddress = "18 Example Lane",
            CompanyAddressLine2 = "0000 Example City",
            CompanyPhone = "12345678",
            CompanyEmail = "elena@example.test",
            InvoiceFolderPath = @"C:\Invoices",
            CalendarId = "primary",
            GoogleAccountEmail = "studio@example.test"
        };

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.NotNull(service.SavedProfile);
        Assert.Equal(
            "18 Example Lane" + Environment.NewLine + "0000 Example City",
            service.SavedProfile.Address);
    }

    [Fact]
    public async Task LoadReportsSavedGoogleAuthorizationWhenLocalTokenFilesExist()
    {
        var service = new FakeAppSettingsService
        {
            Snapshot = new AppSettingsSnapshot(
                null,
                new CalendarSettings("primary", "studio@example.test"),
                GoogleOAuthClientConfigurationStatus.Ready,
                HasGoogleAuthorizationTokens: true)
        };
        var viewModel = new SettingsViewModel(service);

        await viewModel.LoadAsync();

        Assert.Equal(
            "Google authorization: Saved locally. Connect again if Google Calendar access fails.",
            viewModel.GoogleAuthorizationStatusText);
    }

    [Fact]
    public async Task LoadKeepsPrimaryCalendarDefaultWhenSavedCalendarIdIsBlank()
    {
        var service = new FakeAppSettingsService
        {
            Snapshot = new AppSettingsSnapshot(
                null,
                new CalendarSettings("", "studio@example.test"),
                GoogleOAuthClientConfigurationStatus.Ready)
        };
        var viewModel = new SettingsViewModel(service);

        await viewModel.LoadAsync();

        Assert.Equal("primary", viewModel.CalendarId);
        Assert.Equal("studio@example.test", viewModel.GoogleAccountEmail);
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
    public async Task SaveDoesNotPassGoogleOAuthJsonPath()
    {
        var service = new FakeAppSettingsService();
        var viewModel = new SettingsViewModel(service)
        {
            CompanyName = "Maple Key Studio",
            CompanyAddress = "Musikvej 1",
            CompanyPhone = "12345678",
            CompanyEmail = "elena@example.test",
            InvoiceFolderPath = @"C:\Invoices",
            CalendarId = "primary",
            GoogleAccountEmail = "studio@example.test"
        };

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.NotNull(service.SavedProfile);
        Assert.Equal(new CalendarSettings("primary", "studio@example.test"), service.SavedCalendarSettings);
    }

    [Fact]
    public async Task ConnectGoogleCalendarUsesPrimaryWhenCalendarIdIsBlank()
    {
        var service = new FakeAppSettingsService();
        var viewModel = new SettingsViewModel(service)
        {
            CalendarId = "",
            GoogleAccountEmail = "studio@example.test"
        };

        await viewModel.ConnectGoogleCalendarCommand.ExecuteAsync();

        Assert.True(service.ConnectGoogleCalendarWasCalled);
        Assert.Equal("primary", viewModel.CalendarId);
        Assert.Equal(new CalendarSettings("primary", "studio@example.test"), service.ConnectedCalendarSettings);
    }

    [Fact]
    public async Task ConnectGoogleCalendarReportsMissingGoogleAccountEmail()
    {
        var service = new FakeAppSettingsService();
        var viewModel = new SettingsViewModel(service)
        {
            CalendarId = "",
            GoogleAccountEmail = ""
        };

        await viewModel.ConnectGoogleCalendarCommand.ExecuteAsync();

        Assert.False(service.ConnectGoogleCalendarWasCalled);
        Assert.Equal("primary", viewModel.CalendarId);
        Assert.Contains("Google account email", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task ForgetGoogleAuthorizationClearsTokenStoreAndReportsNextAuthorization()
    {
        var service = new FakeAppSettingsService();
        var viewModel = new SettingsViewModel(service);

        await viewModel.ForgetGoogleAuthorizationCommand.ExecuteAsync();

        Assert.True(service.ForgetGoogleAuthorizationWasCalled);
        Assert.Equal(
            "Google authorization cleared. The next connection will ask you to sign in.",
            viewModel.GoogleAuthorizationStatusText);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task ConnectGoogleCalendarReportsSuccessfulAuthorization()
    {
        var service = new FakeAppSettingsService();
        var viewModel = new SettingsViewModel(service)
        {
            CalendarId = "primary",
            GoogleAccountEmail = "studio@example.test"
        };

        await viewModel.ConnectGoogleCalendarCommand.ExecuteAsync();

        Assert.True(service.ConnectGoogleCalendarWasCalled);
        Assert.Equal(new CalendarSettings("primary", "studio@example.test"), service.ConnectedCalendarSettings);
        Assert.Equal("Google Calendar connection succeeded. Save settings to use this calendar.", viewModel.GoogleAuthorizationStatusText);
        Assert.False(viewModel.HasError);
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
        public AppSettingsSnapshot Snapshot { get; init; } = new(null, null, GoogleOAuthClientConfigurationStatus.Missing);

        public CompanyProfile? SavedProfile { get; private set; }

        public CalendarSettings? SavedCalendarSettings { get; private set; }

        public CalendarSettings? ConnectedCalendarSettings { get; private set; }

        public bool ConnectGoogleCalendarWasCalled { get; private set; }

        public bool ForgetGoogleAuthorizationWasCalled { get; private set; }

        public Task<AppSettingsSnapshot> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Snapshot);

        public Task<OperationResult<AppSettingsSnapshot>> SaveAsync(
            CompanyProfile companyProfile,
            CalendarSettings calendarSettings,
            CancellationToken cancellationToken)
        {
            SavedProfile = companyProfile;
            SavedCalendarSettings = calendarSettings;
            return Task.FromResult(OperationResult<AppSettingsSnapshot>.Success(
                new AppSettingsSnapshot(companyProfile, calendarSettings, GoogleOAuthClientConfigurationStatus.Ready)));
        }

        public Task<OperationResult<bool>> ConnectGoogleCalendarAsync(
            CalendarSettings calendarSettings,
            CancellationToken cancellationToken)
        {
            ConnectGoogleCalendarWasCalled = true;
            ConnectedCalendarSettings = calendarSettings;
            return Task.FromResult(OperationResult<bool>.Success(true));
        }

        public Task<OperationResult<bool>> ForgetGoogleAuthorizationAsync(CancellationToken cancellationToken)
        {
            ForgetGoogleAuthorizationWasCalled = true;
            return Task.FromResult(OperationResult<bool>.Success(true));
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

    private static Button? FindButtonByContent(DependencyObject root, string content)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is Button button && string.Equals(button.Content as string, content, StringComparison.Ordinal))
            {
                return button;
            }

            if (child is DependencyObject dependencyObject)
            {
                var match = FindButtonByContent(dependencyObject, content);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        return null;
    }
}
