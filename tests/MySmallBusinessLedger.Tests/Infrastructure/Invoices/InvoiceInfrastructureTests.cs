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
        Assert.Equal(Path.Combine(folder.Path, "000042-Anna-Maria-Anna-Maria-Andersen.pdf"), publish.Value);
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
        var existingFinalPath = Path.Combine(folder.Path, "000007-Anna-Anna-Andersen.pdf");
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
    public async Task QuestPdfInvoiceWriterCreatesNonEmptyPdfWithInvoiceContent()
    {
        using var folder = TestInvoiceFolder.Create();
        var writer = new QuestPdfInvoicePdfWriter();
        var draft = CreateDraft();
        var totals = new InvoiceTotals(Money.Dkk(400m), Money.Dkk(100m), Money.Dkk(500m));
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
