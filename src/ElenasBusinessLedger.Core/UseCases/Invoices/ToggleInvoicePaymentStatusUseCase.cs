using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

// Applies the payment confirmation rule before updating the issued invoice's payment status.
public sealed class ToggleInvoicePaymentStatusUseCase
{
    private readonly IInvoiceRepository invoiceRepository;

    public ToggleInvoicePaymentStatusUseCase(IInvoiceRepository invoiceRepository)
    {
        this.invoiceRepository = invoiceRepository;
    }

    // Applies the confirmation rule before saving a change to an issued invoice's payment status.
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
