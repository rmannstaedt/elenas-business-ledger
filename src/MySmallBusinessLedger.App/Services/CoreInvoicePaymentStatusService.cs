using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.UseCases.Invoices;

namespace MySmallBusinessLedger.App.Services;

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
