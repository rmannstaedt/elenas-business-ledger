using System.Globalization;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.App.ViewModels;

public sealed class InvoiceDisplayModel : ViewModelBase
{
    private PaymentStatus paymentStatus;
    private InvoicePrintStatus printStatus;
    private readonly InvoicePeriod period;

    public InvoiceDisplayModel(IssuedInvoiceSummary invoice)
    {
        InvoiceNumber = invoice.InvoiceNumber;
        StudentShortName = invoice.StudentShortName;
        InvoiceDate = invoice.InvoiceDate;
        period = invoice.Period ?? CreateInvoiceDateFallbackPeriod(invoice.InvoiceDate);
        paymentStatus = invoice.PaymentStatus;
        printStatus = invoice.PrintStatus;
        PdfFilePath = invoice.PdfFilePath ?? string.Empty;
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

    public string PeriodText => FormatPeriod(period);

    public string PaymentStatusText => PaymentStatus == PaymentStatus.Paid ? "Paid" : "Outstanding";

    public string PrintStatusText => printStatus switch
    {
        InvoicePrintStatus.Printed => "Printed",
        InvoicePrintStatus.PrintFailed => "Print failed",
        _ => "Not printed"
    };

    private static InvoicePeriod CreateInvoiceDateFallbackPeriod(DateOnly invoiceDate)
    {
        var firstDay = new DateOnly(invoiceDate.Year, invoiceDate.Month, 1);
        return new InvoicePeriod(firstDay, firstDay.AddMonths(1).AddDays(-1));
    }

    private static string FormatPeriod(InvoicePeriod period)
    {
        if (period.From.Year == period.To.Year && period.From.Month == period.To.Month)
        {
            var danishCulture = CultureInfo.GetCultureInfo("da-DK");
            var text = period.From.ToString("MMMM yyyy", danishCulture);
            return char.ToUpper(text[0], danishCulture) + text[1..];
        }

        return period.From.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
            + " - "
            + period.To.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }
}
