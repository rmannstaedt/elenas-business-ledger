using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Infrastructure.Persistence;

namespace ElenasBusinessLedger.App.Services;

public sealed class InvoicePrinterConfigurationService : IInvoicePrinterConfigurationService
{
    private const string DefaultCalendarId = "primary";
    private readonly IAppSettingsService settingsService;
    private readonly IInvoicePrinterSelectionDialog printerSelectionDialog;

    public InvoicePrinterConfigurationService(
        IAppSettingsService settingsService,
        IInvoicePrinterSelectionDialog printerSelectionDialog)
    {
        this.settingsService = settingsService;
        this.printerSelectionDialog = printerSelectionDialog;
    }

    public async Task<OperationResult<bool>> EnsureConfiguredAsync(CancellationToken cancellationToken)
    {
        var snapshot = await settingsService.LoadAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(snapshot.CompanyProfile?.ConfiguredInvoicePrinterName))
        {
            return OperationResult<bool>.Success(true);
        }

        if (snapshot.CompanyProfile is null)
        {
            return OperationResult<bool>.Failure(new ValidationError(
                "InvoicePrinter.CompanySettingsMissing",
                "Company settings must be saved before choosing an invoice printer."));
        }

        var selectedPrinterName = printerSelectionDialog.SelectPrinterName();
        if (string.IsNullOrWhiteSpace(selectedPrinterName))
        {
            return OperationResult<bool>.Success(false);
        }

        var calendarSettings = snapshot.CalendarSettings ?? new CalendarSettings(DefaultCalendarId, string.Empty);
        var save = await settingsService.SaveAsync(
            snapshot.CompanyProfile with { ConfiguredInvoicePrinterName = selectedPrinterName.Trim() },
            calendarSettings,
            snapshot.EconomySettings,
            cancellationToken);
        if (!save.Succeeded)
        {
            return OperationResult<bool>.Failure(save.Errors.ToArray());
        }

        return OperationResult<bool>.Success(true);
    }
}
