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
                    page.DefaultTextStyle(text => text.FontSize(9).FontFamily("Arial"));

                    page.Header().Element(container => ComposeHeader(container, model));
                    page.Content().PaddingTop(18).Column(column =>
                    {
                        column.Spacing(16);
                        column.Item().Element(container => ComposeParties(container, model));
                        column.Item().Element(container => ComposeServices(container, model));
                        column.Item().Element(container => ComposeLessons(container, model));
                        column.Item().Height(190);
                        column.Item().Element(container => ComposePaymentAndTotals(container, model));
                    });
                    page.Footer().AlignCenter().Text("Tak for undervisningen.").FontColor(Colors.Grey.Darken1);
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
                row.RelativeItem().Text("FAKTURA").FontSize(28).SemiBold();
                row.RelativeItem().AlignRight().Column(company =>
                {
                    company.Item().Text(model.CompanyName).SemiBold().FontSize(13);
                    company.Item().Text("studio.example.test").FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(230).Element(container => ComposeMetadata(container, model));
            });
        });
    }

    private static void ComposeMetadata(IContainer container, InvoicePdfDocumentModel model)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
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
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(column =>
        {
            column.Item().Text(title).SemiBold().FontColor(Colors.Blue.Darken3);
            column.Item().PaddingTop(4).Text(name).SemiBold();
            foreach (var line in addressLines)
            {
                column.Item().Text(line);
            }

            if (!string.IsNullOrWhiteSpace(extraLine))
            {
                column.Item().PaddingTop(3).Text(extraLine);
            }
        });
    }

    private static void ComposeServices(IContainer container, InvoicePdfDocumentModel model)
    {
        container.Column(column =>
        {
            column.Item().Text("Ydelser").FontSize(14).SemiBold();
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
                AddHeader(table, "Antal", true);
                AddHeader(table, "Enhed");
                AddHeader(table, "Pris", true);
                AddHeader(table, "Beløb", true);

                foreach (var service in model.Services)
                {
                    AddBody(table, service.Description);
                    AddBody(table, service.Quantity.ToString(CultureInfo.InvariantCulture), true);
                    AddBody(table, service.Unit);
                    AddBody(table, FormatMoney(service.UnitPriceWithoutVat), true);
                    AddBody(table, FormatMoney(service.TotalWithoutVat), true);
                }

                if (model.PriceAdjustment is not null)
                {
                    AddBody(table, $"Prisjustering: {model.PriceAdjustment.Reason}");
                    AddBody(table, string.Empty, true);
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
            column.Item().Text("Inkluderede lektioner").FontSize(12).SemiBold();
            column.Item().PaddingTop(6).Column(list =>
            {
                foreach (var lesson in model.IncludedLessons)
                {
                    list.Item().Text(lesson);
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
            row.ConstantItem(230).Element(totals => ComposeTotals(totals, model));
        });
    }

    private static void ComposePayment(IContainer container, InvoicePdfDocumentModel model)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(column =>
        {
            column.Item().Text("Betaling").SemiBold().FontColor(Colors.Blue.Darken3);
            column.Item().PaddingTop(4).Text("Betales ved modtagelse.");
            column.Item().Text($"Angiv fakturanr. {model.InvoiceNumber} ved betaling.");
            if (!string.IsNullOrWhiteSpace(model.PaymentInstructions))
            {
                column.Item().PaddingTop(6).Text(model.PaymentInstructions);
            }
        });
    }

    private static void ComposeTotals(IContainer container, InvoicePdfDocumentModel model)
    {
        container.Column(column =>
        {
            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(totals =>
            {
                totals.Item().Row(row =>
                {
                    row.RelativeItem().Text("I alt ekskl. moms");
                    row.RelativeItem().AlignRight().Text(FormatMoney(model.Totals.TotalWithoutVat));
                });

                if (model.Totals.VatAmount.Amount != 0)
                {
                    totals.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text("Moms 25%");
                        row.RelativeItem().AlignRight().Text(FormatMoney(model.Totals.VatAmount));
                    });
                }
                else
                {
                    totals.Item().PaddingTop(7).Text(
                        $"{model.CompanyName} er ikke momsregistreret. Der er derfor ikke opkrævet moms på denne faktura.")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken2);
                }

                totals.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                totals.Item().PaddingTop(7).Row(row =>
                {
                    row.RelativeItem().Text(model.Totals.VatAmount.Amount == 0 ? "I alt" : "I alt inkl. moms").SemiBold();
                    row.RelativeItem().AlignRight().Text(FormatMoney(model.Totals.Total)).FontSize(12).SemiBold();
                });
            });
        });
    }

    private static void AddMetadataRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Element(MetadataLabel).Text(label);
        table.Cell().Element(MetadataValue).Text(value);
    }

    private static void AddHeader(TableDescriptor table, string text, bool alignRight = false)
    {
        var cell = table.Cell().Element(HeaderCell);
        if (alignRight)
        {
            cell.AlignRight().Text(text);
            return;
        }

        cell.Text(text);
    }

    private static void AddBody(TableDescriptor table, string text, bool alignRight = false)
    {
        var cell = table.Cell().Element(BodyCell);
        if (alignRight)
        {
            cell.AlignRight().Text(text);
            return;
        }

        cell.Text(text);
    }

    private static IContainer MetadataLabel(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).DefaultTextStyle(text => text.SemiBold());

    private static IContainer MetadataValue(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).AlignRight();

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingHorizontal(5)
            .PaddingVertical(4)
            .DefaultTextStyle(text => text.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingHorizontal(5)
            .PaddingVertical(5);

    private static string FormatDate(DateOnly date) =>
        date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    private static string FormatMoney(Money money) =>
        string.Create(DanishCulture, $"{money.Amount:N2} {money.Currency}");
}
