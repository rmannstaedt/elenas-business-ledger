using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Money;
using MySmallBusinessLedger.Infrastructure.Invoices.Pdf;
using MySmallBusinessLedger.Infrastructure.Invoices.Printing;

namespace MySmallBusinessLedger.Tests.Infrastructure.Invoices;

public sealed class InvoiceInfrastructureTests
{
    [Fact]
    public async Task FileStorePublishesVerifiedStagedPdfToStableSanitizedFinalPath()
    {
        using var folder = TestInvoiceFolder.Create();
        var fileStore = new LocalInvoiceFileStore(new InvoiceFileStoreOptions(folder.Path));
        var draft = CreateDraft(studentShortName: "Anna/Maria", studentFullName: "Anna Maria Andersen");
        var invoiceNumber = new InvoiceNumber(42);

        var stagingPath = await fileStore.CreateStagingPathAsync(draft, invoiceNumber, CancellationToken.None);
        Assert.True(stagingPath.Succeeded);
        await File.WriteAllBytesAsync(stagingPath.Value!, "%PDF-1.7\ncontent"u8.ToArray(), TestContext.Current.CancellationToken);

        var verify = await fileStore.VerifyStagedPdfAsync(stagingPath.Value!, CancellationToken.None);
        var publish = await fileStore.PublishAsync(stagingPath.Value!, draft, invoiceNumber, CancellationToken.None);

        Assert.True(verify.Succeeded);
        Assert.True(publish.Succeeded);
        Assert.Equal(Path.Combine(folder.Path, "Invoice 042 Juni 2026 - Anna Maria Andersen.pdf"), publish.Value);
        Assert.True(File.Exists(publish.Value));
        Assert.False(File.Exists(stagingPath.Value));
    }

    [Fact]
    public async Task FileStoreDoesNotOverwriteExistingInvoicePdf()
    {
        using var folder = TestInvoiceFolder.Create();
        var fileStore = new LocalInvoiceFileStore(new InvoiceFileStoreOptions(folder.Path));
        var draft = CreateDraft();
        var invoiceNumber = new InvoiceNumber(7);
        var existingFinalPath = Path.Combine(folder.Path, "Invoice 007 Juni 2026 - Anna Andersen.pdf");
        await File.WriteAllTextAsync(existingFinalPath, "existing", TestContext.Current.CancellationToken);

        var stagingPath = await fileStore.CreateStagingPathAsync(draft, invoiceNumber, CancellationToken.None);
        Assert.True(stagingPath.Succeeded);
        await File.WriteAllBytesAsync(stagingPath.Value!, "%PDF-1.7\nnew"u8.ToArray(), TestContext.Current.CancellationToken);

        var publish = await fileStore.PublishAsync(stagingPath.Value!, draft, invoiceNumber, CancellationToken.None);

        Assert.False(publish.Succeeded);
        Assert.Contains(publish.Errors, error => error.Code == "InvoicePdf.FinalFileAlreadyExists");
        Assert.Equal("existing", await File.ReadAllTextAsync(existingFinalPath, TestContext.Current.CancellationToken));
        Assert.True(File.Exists(stagingPath.Value));
    }

    [Fact]
    public async Task FileStoreRejectsMissingOrInvalidStagedPdf()
    {
        using var folder = TestInvoiceFolder.Create();
        var fileStore = new LocalInvoiceFileStore(new InvoiceFileStoreOptions(folder.Path));
        var invalidPath = Path.Combine(folder.Path, "not-a-pdf.pdf");
        await File.WriteAllTextAsync(invalidPath, "not a pdf", TestContext.Current.CancellationToken);

        var missing = await fileStore.VerifyStagedPdfAsync(Path.Combine(folder.Path, "missing.pdf"), CancellationToken.None);
        var invalid = await fileStore.VerifyStagedPdfAsync(invalidPath, CancellationToken.None);

        Assert.False(missing.Succeeded);
        Assert.Contains(missing.Errors, error => error.Code == "InvoicePdf.StagedFileMissing");
        Assert.False(invalid.Succeeded);
        Assert.Contains(invalid.Errors, error => error.Code == "InvoicePdf.StagedFileInvalid");
    }

    [Fact]
    public async Task FileStoreDiscardsOnlyFilesInsideInvoiceStagingFolder()
    {
        using var folder = TestInvoiceFolder.Create();
        var fileStore = new LocalInvoiceFileStore(new InvoiceFileStoreOptions(folder.Path));
        var draft = CreateDraft();
        var invoiceNumber = new InvoiceNumber(7);
        var stagingPath = await fileStore.CreateStagingPathAsync(draft, invoiceNumber, CancellationToken.None);
        Assert.True(stagingPath.Succeeded);
        await File.WriteAllBytesAsync(stagingPath.Value!, "%PDF-1.7\nnew"u8.ToArray(), TestContext.Current.CancellationToken);
        var outsidePath = Path.Combine(folder.Path, "outside.pdf");
        await File.WriteAllTextAsync(outsidePath, "outside", TestContext.Current.CancellationToken);

        var discard = await fileStore.DiscardStagingPathAsync(stagingPath.Value!, CancellationToken.None);
        var outsideDiscard = await fileStore.DiscardStagingPathAsync(outsidePath, CancellationToken.None);
        var missingDiscard = await fileStore.DiscardStagingPathAsync(stagingPath.Value!, CancellationToken.None);

        Assert.True(discard.Succeeded);
        Assert.False(File.Exists(stagingPath.Value));
        Assert.False(outsideDiscard.Succeeded);
        Assert.Contains(outsideDiscard.Errors, error => error.Code == "InvoicePdf.StagingPathOutsideStagingDirectory");
        Assert.True(File.Exists(outsidePath));
        Assert.True(missingDiscard.Succeeded);
    }

    [Fact]
    public async Task QuestPdfInvoiceWriterCreatesNonEmptyPdfWithInvoiceContent()
    {
        using var folder = TestInvoiceFolder.Create();
        var writer = new QuestPdfInvoicePdfWriter();
        var draft = CreateEightLessonDraft();
        var totals = new InvoiceTotals(Money.Dkk(2800m), Money.Dkk(0m), Money.Dkk(2800m));
        var outputPath = Path.Combine(folder.Path, "invoice.pdf");

        var result = await writer.WriteAsync(draft, totals, new InvoiceNumber(12), outputPath, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(File.Exists(outputPath));
        Assert.True(new FileInfo(outputPath).Length > 1000);
        var bytes = await File.ReadAllBytesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);

        var keepSamplePath = Environment.GetEnvironmentVariable("ELENA_LEDGER_KEEP_SAMPLE_PDF");
        if (!string.IsNullOrWhiteSpace(keepSamplePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(keepSamplePath)!);
            File.Copy(outputPath, keepSamplePath, overwrite: true);
        }
    }

    [Fact]
    public async Task QuestPdfInvoiceWriterKeepsNineLessonInvoiceOnOnePage()
    {
        using var folder = TestInvoiceFolder.Create();
        var writer = new QuestPdfInvoicePdfWriter();
        var draft = CreateNineLessonDraft();
        var totals = new InvoiceTotals(Money.Dkk(3150m), Money.Dkk(0m), Money.Dkk(3150m));
        var outputPath = Path.Combine(folder.Path, "invoice.pdf");

        var result = await writer.WriteAsync(draft, totals, new InvoiceNumber(1), outputPath, CancellationToken.None);

        Assert.True(result.Succeeded);
        var bytes = await File.ReadAllBytesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal(1, CountPdfPages(bytes));

        var keepSamplePath = Environment.GetEnvironmentVariable("ELENA_LEDGER_KEEP_NINE_LESSON_SAMPLE_PDF");
        if (!string.IsNullOrWhiteSpace(keepSamplePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(keepSamplePath)!);
            File.Copy(outputPath, keepSamplePath, overwrite: true);
        }
    }

    [Fact]
    public void QuestPdfInvoiceWriterKeepsTenOrFewerIncludedLessonsInOneColumn()
    {
        var lessons = Enumerable.Range(1, 10)
            .Select(day => $"{day:00}-07-2026 14:00")
            .ToArray();

        var columns = QuestPdfInvoicePdfWriter.CreateIncludedLessonColumns(lessons);

        var column = Assert.Single(columns);
        Assert.Equal(lessons, column);
    }

    [Fact]
    public void QuestPdfInvoiceWriterSplitsMoreThanTenIncludedLessonsIntoTwoColumns()
    {
        var lessons = Enumerable.Range(1, 11)
            .Select(day => $"{day:00}-07-2026 14:00")
            .ToArray();

        var columns = QuestPdfInvoicePdfWriter.CreateIncludedLessonColumns(lessons);

        Assert.Equal(2, columns.Count);
        Assert.Equal(lessons[..6], columns[0]);
        Assert.Equal(lessons[6..], columns[1]);
    }

    [Fact]
    public void QuestPdfInvoiceWriterFormatsPaymentAccountLinesForInvoicePaymentBox()
    {
        var lines = QuestPdfInvoicePdfWriter.CreatePaymentBoxDetailLines(
            "001",
            "Betales ved modtagelse.",
            "0000",
            "0000 000 000",
            "0000 0000");

        Assert.Equal(
            [
                "Betales ved modtagelse.",
                "Angiv fakturanr. 001 ved betaling.",
                "Reg. nr.: 0000   Konto nr.: 0000 000 000",
                "MobilePay: 0000 0000"
            ],
            lines);
    }

    [Fact]
    public async Task PrinterDoesNotPrintMissingPdf()
    {
        var launcher = new FakePrintProcessLauncher();
        var printer = new SavedPdfPrinter(launcher, new FakePrinterCatalog(["Office Printer"]));

        var result = await printer.PrintAsync("C:\\missing\\invoice.pdf", "Office Printer", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "InvoicePrinter.PdfMissing");
        Assert.False(launcher.WasCalled);
    }

    [Fact]
    public async Task PrinterLaunchesPrintForExistingPdf()
    {
        using var folder = TestInvoiceFolder.Create();
        var path = Path.Combine(folder.Path, "invoice.pdf");
        await File.WriteAllBytesAsync(path, "%PDF-1.7\ncontent"u8.ToArray(), TestContext.Current.CancellationToken);
        var launcher = new FakePrintProcessLauncher();
        var printer = new SavedPdfPrinter(launcher, new FakePrinterCatalog(["Office Printer"]));

        var result = await printer.PrintAsync(path, "Office Printer", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(path, launcher.PrintedPath);
        Assert.Equal("Office Printer", launcher.PrinterName);
    }

    [Fact]
    public async Task PrinterFailsWhenNoPrinterIsConfigured()
    {
        using var folder = TestInvoiceFolder.Create();
        var path = Path.Combine(folder.Path, "invoice.pdf");
        await File.WriteAllBytesAsync(path, "%PDF-1.7\ncontent"u8.ToArray(), TestContext.Current.CancellationToken);
        var launcher = new FakePrintProcessLauncher();
        var printer = new SavedPdfPrinter(launcher, new FakePrinterCatalog(["Office Printer"]));

        var result = await printer.PrintAsync(path, "", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "InvoicePrinter.NotConfigured");
        Assert.False(launcher.WasCalled);
    }

    [Fact]
    public async Task PrinterFailsWhenConfiguredPrinterIsUnavailable()
    {
        using var folder = TestInvoiceFolder.Create();
        var path = Path.Combine(folder.Path, "invoice.pdf");
        await File.WriteAllBytesAsync(path, "%PDF-1.7\ncontent"u8.ToArray(), TestContext.Current.CancellationToken);
        var launcher = new FakePrintProcessLauncher();
        var printer = new SavedPdfPrinter(launcher, new FakePrinterCatalog(["Office Printer"]));

        var result = await printer.PrintAsync(path, "New Printer", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "InvoicePrinter.Unavailable");
        Assert.False(launcher.WasCalled);
    }

    private static InvoiceDraft CreateDraft(
        string studentShortName = "Anna",
        string studentFullName = "Anna Andersen")
    {
        return InvoiceDraft.Create(
            new DateOnly(2026, 6, 16),
            new CompanySnapshot("Elena Musik", "Musikvej 1", "12345678"),
            new StudentSnapshot(studentShortName, studentFullName, "Testvej 1"),
            [
                new InvoiceLineDraft(
                    new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2)),
                    "Standard",
                    Money.Dkk(400m))
            ],
            priceAdjustment: null);
    }

    private static InvoiceDraft CreateEightLessonDraft()
    {
        var dates = new[]
        {
            new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 6, 4, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 6, 8, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 6, 11, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 6, 15, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 6, 18, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 6, 25, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 6, 29, 14, 0, 0, TimeSpan.FromHours(2))
        };

        return InvoiceDraft.Create(
            new DateOnly(2026, 6, 23),
            new CompanySnapshot(
                "Maple Key Studio",
                "18 Example Lane, 0000 Example City",
                null,
                string.Empty,
                "Reg.nr. 1234 Kontonr. 5678901234",
                "MobilePay 123456"),
            new StudentSnapshot("Mira", "Mira Vale", "42 Sample Lane" + Environment.NewLine + "0000 Example City"),
            dates.Select(date => new InvoiceLineDraft(
                date,
                "Tilbud 2 - 2026: 2 lektioner/uge",
                Money.Dkk(350m))).ToArray(),
            priceAdjustment: null,
            period: new InvoicePeriod(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));
    }

    private static InvoiceDraft CreateNineLessonDraft()
    {
        var dates = new[]
        {
            new DateTimeOffset(2026, 7, 2, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 7, 6, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 7, 9, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 7, 13, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 7, 16, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 7, 20, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 7, 23, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 7, 27, 14, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 7, 30, 14, 0, 0, TimeSpan.FromHours(2))
        };

        return InvoiceDraft.Create(
            new DateOnly(2026, 6, 26),
            new CompanySnapshot(
                "Maple Key Studio",
                "18 Example Lane, 0000 Example City",
                null,
                string.Empty,
                "0000",
                "0000 000 000",
                "0000 0000"),
            new StudentSnapshot("Mira", "Mira Vale", "42 Sample Lane" + Environment.NewLine + "0000 Example City"),
            dates.Select(date => new InvoiceLineDraft(
                date,
                "Tilbud 2 - 2026: 2 lektioner/uge",
                Money.Dkk(350m))).ToArray(),
            priceAdjustment: null,
            period: new InvoicePeriod(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)));
    }

    private static int CountPdfPages(byte[] bytes)
    {
        var pdfText = System.Text.Encoding.Latin1.GetString(bytes);
        return System.Text.RegularExpressions.Regex.Matches(
            pdfText,
            @"/Type\s*/Page\b",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant).Count;
    }

    private sealed class FakePrintProcessLauncher : IPrintProcessLauncher
    {
        public bool WasCalled { get; private set; }

        public string? PrintedPath { get; private set; }

        public string? PrinterName { get; private set; }

        public Task PrintAsync(string pdfFilePath, string printerName, CancellationToken cancellationToken)
        {
            WasCalled = true;
            PrintedPath = pdfFilePath;
            PrinterName = printerName;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePrinterCatalog(IReadOnlyList<string> printerNames) : IPrinterCatalog
    {
        public Task<IReadOnlyList<string>> ListPrinterNamesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(printerNames);
    }

    private sealed class TestInvoiceFolder : IDisposable
    {
        private TestInvoiceFolder(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TestInvoiceFolder Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ElenaLedgerInvoiceTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TestInvoiceFolder(path);
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
