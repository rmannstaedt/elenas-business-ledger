using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.UseCases.Invoices;

namespace ElenasBusinessLedger.App.Services;

// Adapts the payment status use case to the result shown by the dashboard.
public sealed class CoreInvoicePaymentStatusService : IInvoicePaymentStatusService
{
    private readonly ToggleInvoicePaymentStatusUseCase useCase;

    public CoreInvoicePaymentStatusService(ToggleInvoicePaymentStatusUseCase useCase)
    {
        this.useCase = useCase;
    }

    public async Task<OperationResult<PaymentStatus>> ToggleAsync(
        InvoiceNumber invoiceNumber,
        bool confirmMarkOutstanding,
        CancellationToken cancellationToken)
    {
        try
        {
            return await useCase.ExecuteAsync(invoiceNumber, confirmMarkOutstanding, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<PaymentStatus>.Failure(
                new ValidationError("Invoice.PaymentStatusUpdateFailed", ex.Message));
        }
    }
}
