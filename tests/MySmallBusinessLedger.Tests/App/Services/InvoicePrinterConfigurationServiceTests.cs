using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.Tests.App.Services;

public sealed class InvoicePrinterConfigurationServiceTests
{
    [Fact]
    public async Task EnsureConfiguredSavesSelectedPrinterWhenNoneIsRemembered()
    {
        var settings = new FakeAppSettingsService
        {
            Snapshot = new AppSettingsSnapshot(
                CreateCompanyProfile(configuredInvoicePrinterName: ""),
                new CalendarSettings("primary", "elena@example.test"),
                GoogleOAuthClientConfigurationStatus.Ready)
        };
        var selector = new FakeInvoicePrinterSelectionDialog { SelectedPrinterName = "Office Printer" };
        var service = new InvoicePrinterConfigurationService(settings, selector);

        var result = await service.EnsureConfiguredAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.Value);
        Assert.True(selector.WasShown);
        Assert.NotNull(settings.SavedProfile);
        Assert.Equal("Office Printer", settings.SavedProfile.ConfiguredInvoicePrinterName);
        Assert.Equal(new CalendarSettings("primary", "elena@example.test"), settings.SavedCalendarSettings);
    }

    [Fact]
    public async Task EnsureConfiguredDoesNotOpenSelectorWhenPrinterIsRemembered()
    {
        var settings = new FakeAppSettingsService
        {
            Snapshot = new AppSettingsSnapshot(
                CreateCompanyProfile(configuredInvoicePrinterName: "Office Printer"),
                new CalendarSettings("primary", "elena@example.test"),
                GoogleOAuthClientConfigurationStatus.Ready)
        };
        var selector = new FakeInvoicePrinterSelectionDialog { SelectedPrinterName = "Other Printer" };
        var service = new InvoicePrinterConfigurationService(settings, selector);

        var result = await service.EnsureConfiguredAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.Value);
        Assert.False(selector.WasShown);
        Assert.Null(settings.SavedProfile);
    }

    [Fact]
    public async Task EnsureConfiguredReturnsFalseWhenPrinterSelectionIsCancelled()
    {
        var settings = new FakeAppSettingsService
        {
            Snapshot = new AppSettingsSnapshot(
                CreateCompanyProfile(configuredInvoicePrinterName: ""),
                new CalendarSettings("primary", "elena@example.test"),
                GoogleOAuthClientConfigurationStatus.Ready)
        };
        var selector = new FakeInvoicePrinterSelectionDialog { SelectedPrinterName = null };
        var service = new InvoicePrinterConfigurationService(settings, selector);

        var result = await service.EnsureConfiguredAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(result.Value);
        Assert.True(selector.WasShown);
        Assert.Null(settings.SavedProfile);
    }

    private static CompanyProfile CreateCompanyProfile(string configuredInvoicePrinterName) =>
        CompanyProfile.Create(
            "Maple Key Studio",
            "18 Example Lane",
            phone: "12345678",
            email: "elena@example.test",
            cvr: null,
            vatRegistrationDate: null,
            vatStatus: VatStatus.NotRegistered,
            invoiceFolderPath: @"C:\Invoices",
            configuredInvoicePrinterName);

    private sealed class FakeInvoicePrinterSelectionDialog : IInvoicePrinterSelectionDialog
    {
        public string? SelectedPrinterName { get; init; }

        public bool WasShown { get; private set; }

        public string? SelectPrinterName()
        {
            WasShown = true;
            return SelectedPrinterName;
        }
    }

    private sealed class FakeAppSettingsService : IAppSettingsService
    {
        public AppSettingsSnapshot Snapshot { get; init; } = new(null, null, GoogleOAuthClientConfigurationStatus.Missing);

        public CompanyProfile? SavedProfile { get; private set; }

        public CalendarSettings? SavedCalendarSettings { get; private set; }

        public EconomySettings? SavedEconomySettings { get; private set; }

        public Task<AppSettingsSnapshot> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Snapshot);

        public Task<OperationResult<AppSettingsSnapshot>> SaveAsync(
            CompanyProfile companyProfile,
            CalendarSettings calendarSettings,
            EconomySettings? economySettings,
            CancellationToken cancellationToken)
        {
            SavedProfile = companyProfile;
            SavedCalendarSettings = calendarSettings;
            SavedEconomySettings = economySettings;
            return Task.FromResult(OperationResult<AppSettingsSnapshot>.Success(
                Snapshot with
                {
                    CompanyProfile = companyProfile,
                    CalendarSettings = calendarSettings,
                    EconomySettings = economySettings
                }));
        }

        public Task<OperationResult<bool>> ConnectGoogleCalendarAsync(
            CalendarSettings calendarSettings,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<bool>.Success(true));

        public Task<OperationResult<bool>> ForgetGoogleAuthorizationAsync(CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<bool>.Success(true));
    }
}
