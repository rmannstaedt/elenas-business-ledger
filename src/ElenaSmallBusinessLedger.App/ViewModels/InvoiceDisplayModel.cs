using System.Globalization;
using ElenaSmallBusinessLedger.Core.Invoices;

namespace ElenaSmallBusinessLedger.App.ViewModels;

public sealed class InvoiceDisplayModel : ViewModelBase
{
    private PaymentStatus paymentStatus;

    public InvoiceDisplayModel(IssuedInvoiceSummary invoice)
    {
        InvoiceNumber = invoice.InvoiceNumber;
        StudentShortName = invoice.StudentShortName;
        InvoiceDate = invoice.InvoiceDate;
        paymentStatus = invoice.PaymentStatus;
        PdfFilePath = invoice.PdfFilePath;
    }

    public InvoiceNumber InvoiceNumber { get; }

    public string StudentShortName { get; }

    public DateOnly InvoiceDate { get; }

    public PaymentStatus PaymentStatus
    {
        get => paymentStatus;
        set
        {
            if (SetProperty(ref paymentStatus, value))
            {
                OnPropertyChanged(nameof(PaymentStatusText));
            }
        }
    }

    public string PdfFilePath { get; }

    public string InvoiceNumberText => InvoiceNumber.ToString();

    public string InvoiceDateText => InvoiceDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    public string PaymentStatusText => PaymentStatus == PaymentStatus.Paid ? "Paid" : "Outstanding";
}
