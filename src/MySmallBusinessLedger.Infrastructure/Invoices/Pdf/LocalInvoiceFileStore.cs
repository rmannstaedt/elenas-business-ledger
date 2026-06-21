using System.Text;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Infrastructure.Invoices.Pdf;

public sealed class LocalInvoiceFileStore : IInvoiceFileStore
{
    private static readonly byte[] PdfHeader = "%PDF"u8.ToArray();
    private readonly string invoiceFolderPath;

    public LocalInvoiceFileStore(InvoiceFileStoreOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.InvoiceFolderPath))
        {
            throw new ArgumentException("Invoice folder path is required.", nameof(options));
        }

        invoiceFolderPath = Path.GetFullPath(options.InvoiceFolderPath);
    }

    public Task<OperationResult<string>> CreateStagingPathAsync(
        InvoiceDraft draft,
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(invoiceFolderPath);
            var stagingDirectory = Path.Combine(invoiceFolderPath, ".staging");
            Directory.CreateDirectory(stagingDirectory);

            var stagingPath = Path.Combine(
                stagingDirectory,
                $"{invoiceNumber}-{Guid.NewGuid():N}.pdf");

            return Task.FromResult(OperationResult<string>.Success(stagingPath));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Task.FromResult(Failure("InvoicePdf.StagingPathFailed", "Could not create a staging path for the invoice PDF."));
        }
    }

    public async Task<OperationResult<string>> VerifyStagedPdfAsync(string stagingPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(stagingPath))
        {
            return Failure("InvoicePdf.StagedFileMissing", "The staged invoice PDF was not found.");
        }

        var fileInfo = new FileInfo(stagingPath);
        if (fileInfo.Length <= PdfHeader.Length)
        {
            return Failure("InvoicePdf.StagedFileInvalid", "The staged invoice PDF is empty or invalid.");
        }

        var header = new byte[PdfHeader.Length];
        await using var stream = File.OpenRead(stagingPath);
        var bytesRead = await stream.ReadAsync(header, cancellationToken);

        if (bytesRead != PdfHeader.Length || !header.SequenceEqual(PdfHeader))
        {
            return Failure("InvoicePdf.StagedFileInvalid", "The staged invoice PDF is empty or invalid.");
        }

        return OperationResult<string>.Success(stagingPath);
    }

    public Task<OperationResult<string>> PublishAsync(
        string stagingPath,
        InvoiceDraft draft,
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(stagingPath))
        {
            return Task.FromResult(Failure("InvoicePdf.StagedFileMissing", "The staged invoice PDF was not found."));
        }

        try
        {
            Directory.CreateDirectory(invoiceFolderPath);
            var finalPath = Path.Combine(invoiceFolderPath, CreateFinalFileName(draft, invoiceNumber));

            if (File.Exists(finalPath))
            {
                return Task.FromResult(Failure("InvoicePdf.FinalFileAlreadyExists", "An invoice PDF with the same final file name already exists."));
            }

            File.Move(stagingPath, finalPath, overwrite: false);
            return Task.FromResult(OperationResult<string>.Success(finalPath));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Task.FromResult(Failure("InvoicePdf.PublishFailed", "Could not publish the invoice PDF to the invoice folder."));
        }
    }

    private static string CreateFinalFileName(InvoiceDraft draft, InvoiceNumber invoiceNumber) =>
        $"{invoiceNumber}-{SanitizeFileNamePart(draft.Student.ShortName)}-{SanitizeFileNamePart(draft.Student.FullName)}.pdf";

    private static string SanitizeFileNamePart(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new StringBuilder(value.Length);
        var lastWasDash = false;

        foreach (var character in value.Trim())
        {
            var replacement = invalidCharacters.Contains(character) || char.IsWhiteSpace(character)
                ? '-'
                : character;

            if (replacement == '-')
            {
                if (lastWasDash)
                {
                    continue;
                }

                lastWasDash = true;
            }
            else
            {
                lastWasDash = false;
            }

            builder.Append(replacement);
        }

        var sanitized = builder.ToString().Trim('-');
        return sanitized.Length == 0 ? "invoice" : sanitized;
    }

    private static OperationResult<string> Failure(string code, string message) =>
        OperationResult<string>.Failure(new ValidationError(code, message));
}
