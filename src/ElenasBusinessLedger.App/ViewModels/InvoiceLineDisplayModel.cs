using System.Globalization;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.App.ViewModels;

public sealed class InvoiceLineDisplayModel : ViewModelBase
{
    private bool isIncluded = true;

    public InvoiceLineDisplayModel(InvoiceLineDraft line)
    {
        Line = line;
    }

    public InvoiceLineDraft Line { get; }

    public bool IsIncluded
    {
        get => isIncluded;
        set
        {
            if (SetProperty(ref isIncluded, value))
            {
                InclusionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string LessonDateTimeText => Line.HasKnownLessonTime
        ? Line.LessonDateTime.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)
        : Line.LessonDateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    public string PlanName => Line.PlanName;

    public string IncludeAutomationName => $"Include lesson {LessonDateTimeText} {PlanName}";

    public string PriceWithoutVatText =>
        string.Create(CultureInfo.GetCultureInfo("da-DK"), $"{Line.PriceWithoutVat.Amount:N2} {Line.PriceWithoutVat.Currency}");

    public event EventHandler? InclusionChanged;
}
