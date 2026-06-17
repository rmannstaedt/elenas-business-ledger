using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Invoices;

public sealed class ToggleInvoicePaymentStatusUseCase
{
    private readonly IInvoiceRepository invoiceRepository;

    public ToggleInvoicePaymentStatusUseCase(IInvoiceRepository invoiceRepository)
    {
        this.invoiceRepository = invoiceRepository;
    }

    public async Task<OperationResult<PaymentStatus>> ExecuteAsync(
        InvoiceNumber invoiceNumber,
        bool confirmMarkOutstanding,
        CancellationToken cancellationToken)
    {
        var currentStatus = await invoiceRepository.GetPaymentStatusAsync(invoiceNumber, cancellationToken);
        var result = PaymentStatusRules.Toggle(currentStatus, confirmMarkOutstanding);
        if (!result.Succeeded)
        {
            return result;
        }

        await invoiceRepository.SavePaymentStatusAsync(invoiceNumber, result.Value, cancellationToken);
        return result;
    }
}
