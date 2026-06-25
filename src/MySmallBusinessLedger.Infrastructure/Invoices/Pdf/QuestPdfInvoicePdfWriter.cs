using System.Globalization;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Money;
using MySmallBusinessLedger.Core.Ports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MySmallBusinessLedger.Infrastructure.Invoices.Pdf;

public sealed class QuestPdfInvoicePdfWriter : IInvoicePdfWriter
{
    private const string InvoiceFontFamily = "Segoe UI";
    private const float BaseFontSize = 9.5f;
    private const float BaseLineHeight = 1.12f;
    private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");

    static QuestPdfInvoicePdfWriter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<OperationResult<string>> WriteAsync(
        InvoiceDraft draft,
        InvoiceTotals totals,
        InvoiceNumber invoiceNumber,
        string stagingPath,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(stagingPath))!);

            var model = InvoicePdfDocumentModel.Create(draft, totals, invoiceNumber);

            Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(34);
                    page.DefaultTextStyle(text => text
                        .FontSize(BaseFontSize)
                        .FontFamily(InvoiceFontFamily)
                        .LineHeight(BaseLineHeight));

                    page.Header().Element(container => ComposeHeader(container, model));
                    page.Content().PaddingTop(18).Column(column =>
                    {
                        column.Spacing(16);
                        column.Item().Element(container => ComposeParties(container, model));
                        column.Item().PaddingTop(8).Element(container => ComposeServices(container, model));
                        column.Item().Element(container => ComposeLessons(container, model));
                        column.Item().Height(145);
                        column.Item().Element(container => ComposePaymentAndTotals(container, model));
                    });
                    page.Footer().Column(footer =>
                    {
                        footer.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        footer.Item().PaddingTop(8).AlignCenter().Text("Tak for undervisningen.").FontColor(Colors.Grey.Darken1);
                    });
                });
            }).GeneratePdf(stagingPath);

            return Task.FromResult(OperationResult<string>.Success(stagingPath));
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(OperationResult<string>.Failure(
                new ValidationError("InvoicePdf.WriteFailed", "Could not write the invoice PDF.")));
        }
    }

    private static void ComposeHeader(IContainer container, InvoicePdfDocumentModel model)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("FAKTURA").FontSize(27.5f);
                row.RelativeItem().AlignRight().Column(company =>
                {
                    company.Item().Text(model.CompanyName).SemiBold().FontSize(10.5f);
                    company.Item().Text("studio.example.test").FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(185).Element(container => ComposeMetadata(container, model));
            });
        });
    }

    private static void ComposeMetadata(IContainer container, InvoicePdfDocumentModel model)
    {
        container.PaddingLeft(10).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(1.2f);
            });

            AddMetadataRow(table, "Fakturanr.", model.InvoiceNumber);
            AddMetadataRow(table, "Fakturadato", FormatDate(model.InvoiceDate));
            AddMetadataRow(table, "Forfald", "Ved modtagelse");
            AddMetadataRow(table, "Periode", model.Period);
        });
    }

    private static void ComposeParties(IContainer container, InvoicePdfDocumentModel model)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(card => ComposeAddressCard(
                card,
                "Fra",
                model.CompanyName,
                model.CompanyAddressLines,
                string.IsNullOrWhiteSpace(model.CompanyCvr) ? null : $"CVR {model.CompanyCvr}"));
            row.ConstantItem(18);
            row.RelativeItem().Element(card => ComposeAddressCard(
                card,
                "Til",
                model.StudentFullName,
                model.StudentAddressLines,
                null));
        });
    }

    private static void ComposeAddressCard(
        IContainer container,
        string title,
        string name,
        IReadOnlyList<string> addressLines,
        string? extraLine)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .CornerRadius(7)
            .Column(column =>
        {
            column.Item()
                .Background(Colors.BlueGrey.Lighten5)
                .PaddingHorizontal(10)
                .PaddingVertical(7)
                .Text(title)
                .SemiBold();
            column.Item().PaddingHorizontal(10).PaddingTop(10).Column(body =>
            {
                body.Spacing(3);
                body.Item().Text(name).SemiBold();
                foreach (var line in addressLines)
                {
                    body.Item().Text(line);
                }

                if (!string.IsNullOrWhiteSpace(extraLine))
                {
                    body.Item().Text(extraLine);
                }
            });

            column.Item().Height(18);
        });
    }

    private static void ComposeServices(IContainer container, InvoicePdfDocumentModel model)
    {
        container.Column(column =>
        {
            column.Item().Text("Ydelser").FontSize(13).SemiBold();
            column.Item().PaddingTop(3).Text("Klaverundervisning leveret i den angivne periode.")
                .FontSize(8.5f)
                .FontColor(Colors.Grey.Darken2);
            column.Item().Height(5);
            foreach (var note in model.PlanChangeNotes)
            {
                column.Item().PaddingTop(4).Text(note).FontColor(Colors.Grey.Darken2);
            }

            column.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(5);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1.6f);
                });

                AddHeader(table, "Beskrivelse");
                AddHeader(table, "Antal", Alignment.Center);
                AddHeader(table, "Enhed");
                AddHeader(table, "Pris", true);
                AddHeader(table, "Beløb", true);

                foreach (var service in model.Services)
                {
                    AddServiceDescriptionBody(table, service.Description, service.Detail);
                    AddBody(table, service.Quantity.ToString(CultureInfo.InvariantCulture), Alignment.Center);
                    AddBody(table, service.Unit);
                    AddBody(table, FormatMoney(service.UnitPriceWithoutVat), true);
                    AddBody(table, FormatMoney(service.TotalWithoutVat), true);
                }

                if (model.PriceAdjustment is not null)
                {
                    AddBody(table, $"Prisjustering: {model.PriceAdjustment.Reason}");
                    AddBody(table, string.Empty, Alignment.Center);
                    AddBody(table, string.Empty);
                    AddBody(table, string.Empty, true);
                    AddBody(table, FormatMoney(model.PriceAdjustment.Amount), true);
                }
            });
        });
    }

    private static void ComposeLessons(IContainer container, InvoicePdfDocumentModel model)
    {
        container.Column(column =>
        {
            column.Item().Text("Inkluderede lektioner").FontSize(10.5f).SemiBold();
            column.Item().PaddingTop(6).Column(list =>
            {
                list.Spacing(4);
                foreach (var lesson in model.IncludedLessons)
                {
                    list.Item().Text(lesson).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                }
            });
        });
    }

    private static void ComposePaymentAndTotals(IContainer container, InvoicePdfDocumentModel model)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(payment => ComposePayment(payment, model));
            row.ConstantItem(22);
            row.ConstantItem(230).PaddingTop(2).Element(totals => ComposeTotals(totals, model));
        });
    }

    private static void ComposePayment(IContainer container, InvoicePdfDocumentModel model)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .CornerRadius(7)
            .Background(Colors.Grey.Lighten5)
            .MinHeight(92)
            .Padding(12)
            .Column(column =>
        {
            column.Item().Text("Betaling").SemiBold();
            column.Item()
                .PaddingTop(7)
                .DefaultTextStyle(text => text.FontColor(Colors.Grey.Darken2).LineHeight(1.23f))
                .Column(details =>
            {
                details.Spacing(4);
                foreach (var line in CreatePaymentBoxDetailLines(
                    model.InvoiceNumber,
                    model.PaymentInstructions,
                    model.BankRegistrationNumber,
                    model.BankAccountNumber,
                    model.MobilePayAccount))
                {
                    details.Item().Text(line);
                }
            });
        });
    }

    internal static IReadOnlyList<string> CreatePaymentBoxDetailLines(
        string invoiceNumber,
        string paymentInstructions,
        string bankRegistrationNumber,
        string bankAccountNumber,
        string mobilePayAccount)
    {
        var lines = new List<string>
        {
            string.IsNullOrWhiteSpace(paymentInstructions) ? "Betales ved modtagelse." : paymentInstructions.Trim(),
            $"Angiv fakturanr. {invoiceNumber} ved betaling."
        };

        var bankParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(bankRegistrationNumber))
        {
            bankParts.Add($"Reg. nr.: {bankRegistrationNumber.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(bankAccountNumber))
        {
            bankParts.Add($"Konto nr.: {bankAccountNumber.Trim()}");
        }

        if (bankParts.Count > 0)
        {
            lines.Add(string.Join("   ", bankParts));
        }

        if (!string.IsNullOrWhiteSpace(mobilePayAccount))
        {
            lines.Add($"MobilePay: {mobilePayAccount.Trim()}");
        }

        return lines;
    }

    private static void ComposeTotals(IContainer container, InvoicePdfDocumentModel model)
    {
        container.Column(column =>
        {
            if (model.Totals.VatAmount.Amount != 0)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("I alt ekskl. moms");
                    row.RelativeItem().AlignRight().Text(FormatMoney(model.Totals.TotalWithoutVat));
                });

                column.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text("Moms 25%");
                    row.RelativeItem().AlignRight().Text(FormatMoney(model.Totals.VatAmount));
                });
            }

            column.Item().LineHorizontal(1).LineColor(Colors.BlueGrey.Darken1);
            column.Item().PaddingTop(9).Row(row =>
            {
                row.RelativeItem().Text("I alt til betaling").FontSize(12.5f).SemiBold();
                row.RelativeItem().AlignRight().Text(FormatMoney(model.Totals.Total)).FontSize(12.5f).SemiBold();
            });

            if (model.Totals.VatAmount.Amount == 0)
            {
                column.Item().PaddingTop(12).Text(
                    $"{model.CompanyName} er ikke momsregistreret. Der er derfor ikke opkrævet moms på denne faktura.")
                    .FontSize(8.5f)
                    .FontColor(Colors.Grey.Darken2);
            }
        });
    }

    private static void AddMetadataRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Element(MetadataLabel).Text(label);
        table.Cell().Element(MetadataValue).Text(value);
    }

    private static void AddHeader(TableDescriptor table, string text, bool alignRight = false) =>
        AddHeader(table, text, alignRight ? Alignment.Right : Alignment.Left);

    private static void AddHeader(TableDescriptor table, string text, Alignment alignment)
    {
        var cell = table.Cell().Element(HeaderCell);
        if (alignment == Alignment.Right)
        {
            cell.AlignRight().Text(text);
            return;
        }

        if (alignment == Alignment.Center)
        {
            cell.AlignCenter().Text(text);
            return;
        }

        cell.Text(text);
    }

    private static void AddBody(TableDescriptor table, string text, bool alignRight = false) =>
        AddBody(table, text, alignRight ? Alignment.Right : Alignment.Left);

    private static void AddBody(TableDescriptor table, string text, Alignment alignment)
    {
        var cell = table.Cell().Element(BodyCell).AlignMiddle();
        if (alignment == Alignment.Right)
        {
            cell.AlignRight().Text(text);
            return;
        }

        if (alignment == Alignment.Center)
        {
            cell.AlignCenter().Text(text);
            return;
        }

        cell.Text(text);
    }

    private static void AddServiceDescriptionBody(TableDescriptor table, string description, string detail)
    {
        table.Cell().Element(BodyCell).Column(column =>
        {
            column.Spacing(2);
            column.Item().Text(description);
            if (!string.IsNullOrWhiteSpace(detail))
            {
                column.Item().Text(detail).FontColor(Colors.Grey.Darken2);
            }
        });
    }

    private static IContainer MetadataLabel(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingLeft(4)
            .PaddingVertical(3)
            .DefaultTextStyle(text => text.FontFamily(InvoiceFontFamily).FontColor(Colors.Grey.Darken1));

    private static IContainer MetadataValue(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(3)
            .AlignRight()
            .DefaultTextStyle(text => text.FontFamily(InvoiceFontFamily).SemiBold());

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingHorizontal(5)
            .PaddingVertical(4)
            .DefaultTextStyle(text => text.FontFamily(InvoiceFontFamily).SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingHorizontal(5)
            .PaddingVertical(7);

    private enum Alignment
    {
        Left,
        Center,
        Right
    }

    private static string FormatDate(DateOnly date) =>
        date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    private static string FormatMoney(Money money) =>
        string.Create(DanishCulture, $"{money.Amount:N2} {money.Currency}");
}
