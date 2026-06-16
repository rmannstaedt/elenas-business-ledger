using System.Globalization;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Money;
using ElenaSmallBusinessLedger.Core.Ports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ElenaSmallBusinessLedger.Infrastructure.Invoices.Pdf;

public sealed class QuestPdfInvoicePdfWriter : IInvoicePdfWriter
{
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

            Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(text => text.FontSize(10).FontFamily("Arial"));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("Faktura").FontSize(24).SemiBold();
                        column.Item().Text($"Fakturanr. {invoiceNumber}");
                        column.Item().Text($"Fakturadato {FormatDate(draft.InvoiceDate)}");
                    });

                    page.Content().PaddingTop(25).Column(column =>
                    {
                        column.Spacing(18);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(company =>
                            {
                                company.Item().Text("Fra").SemiBold();
                                company.Item().Text(draft.Company.Name);
                                company.Item().Text(draft.Company.Address);
                                if (!string.IsNullOrWhiteSpace(draft.Company.Cvr))
                                {
                                    company.Item().Text($"CVR {draft.Company.Cvr}");
                                }
                            });

                            row.RelativeItem().Column(student =>
                            {
                                student.Item().Text("Til").SemiBold();
                                student.Item().Text(draft.Student.FullName);
                                student.Item().Text(draft.Student.Address);
                            });
                        });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Lektion");
                                header.Cell().Element(HeaderCell).Text("Plan");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Beløb");
                            });

                            foreach (var line in draft.Lines)
                            {
                                table.Cell().Element(BodyCell).Text(FormatDateTime(line.LessonDateTime));
                                table.Cell().Element(BodyCell).Text(line.PlanName);
                                table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(line.PriceWithoutVat));
                            }

                            if (draft.PriceAdjustment is not null)
                            {
                                table.Cell().Element(BodyCell).Text("Prisjustering");
                                table.Cell().Element(BodyCell).Text(draft.PriceAdjustment.Reason);
                                table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(draft.PriceAdjustment.Amount));
                            }
                        });

                        column.Item().AlignRight().Column(totalsColumn =>
                        {
                            totalsColumn.Item().Text($"I alt ekskl. moms: {FormatMoney(totals.TotalWithoutVat)}");
                            if (totals.VatAmount.Amount != 0)
                            {
                                totalsColumn.Item().Text($"Moms 25%: {FormatMoney(totals.VatAmount)}");
                            }

                            totalsColumn.Item().Text($"I alt: {FormatMoney(totals.Total)}").FontSize(14).SemiBold();
                        });
                    });

                    page.Footer().AlignCenter().Text("Tak for undervisningen.");
                });
            }).GeneratePdf(stagingPath);

            return Task.FromResult(OperationResult<string>.Success(stagingPath));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Task.FromResult(OperationResult<string>.Failure(
                new ValidationError("InvoicePdf.WriteFailed", "Could not write the invoice PDF.")));
        }
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(6).DefaultTextStyle(text => text.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6);

    private static string FormatDate(DateOnly date) =>
        date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    private static string FormatDateTime(DateTimeOffset dateTime) =>
        dateTime.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);

    private static string FormatMoney(Money money) =>
        string.Create(CultureInfo.GetCultureInfo("da-DK"), $"{money.Amount:N2} {money.Currency}");
}
