using System.Collections.ObjectModel;
using System.Globalization;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Money;

namespace ElenaSmallBusinessLedger.App.ViewModels;

public sealed class InvoiceViewerViewModel
{
    public InvoiceViewerViewModel(IssuedInvoice invoice)
    {
        InvoiceNumberText = invoice.InvoiceNumber.ToString();
        InvoiceDateText = invoice.InvoiceDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        CompanyName = invoice.Company.Name;
        CompanyAddress = invoice.Company.Address;
        StudentName = invoice.Student.FullName;
        StudentAddress = invoice.Student.Address;
        TotalWithoutVatText = FormatMoney(invoice.Totals.TotalWithoutVat);
        VatAmountText = FormatMoney(invoice.Totals.VatAmount);
        TotalText = FormatMoney(invoice.Totals.Total);
        PdfFilePath = invoice.PdfFilePath;

        foreach (var line in invoice.Lines.OrderBy(line => line.LessonDateTime))
        {
            Lines.Add(new InvoiceViewerLineDisplayModel(line));
        }
    }

    public string InvoiceNumberText { get; }

    public string InvoiceDateText { get; }

    public string CompanyName { get; }

    public string CompanyAddress { get; }

    public string StudentName { get; }

    public string StudentAddress { get; }

    public string TotalWithoutVatText { get; }

    public string VatAmountText { get; }

    public string TotalText { get; }

    public string PdfFilePath { get; }

    public ObservableCollection<InvoiceViewerLineDisplayModel> Lines { get; } = [];

    private static string FormatMoney(Money money) =>
        string.Create(CultureInfo.GetCultureInfo("da-DK"), $"{money.Amount:N2} {money.Currency}");
}

public sealed class InvoiceViewerLineDisplayModel
{
    public InvoiceViewerLineDisplayModel(IssuedInvoiceLine line)
    {
        LessonDateTimeText = line.LessonDateTime.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);
        PlanName = line.PlanName;
        PriceWithoutVatText = string.Create(
            CultureInfo.GetCultureInfo("da-DK"),
            $"{line.PriceWithoutVat.Amount:N2} {line.PriceWithoutVat.Currency}");
    }

    public string LessonDateTimeText { get; }

    public string PlanName { get; }

    public string PriceWithoutVatText { get; }
}
