using System.Globalization;
using System.Text;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Infrastructure.Invoices.Pdf;

public sealed class LocalInvoiceFileStore : IInvoiceFileStore
{
    private static readonly byte[] PdfHeader = "%PDF"u8.ToArray();
    private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");
    private readonly string invoiceFolderPath;
    private readonly string stagingFolderPath;

    public LocalInvoiceFileStore(InvoiceFileStoreOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.InvoiceFolderPath))
        {
            throw new ArgumentException("Invoice folder path is required.", nameof(options));
        }

        invoiceFolderPath = Path.GetFullPath(options.InvoiceFolderPath);
        stagingFolderPath = Path.GetFullPath(Path.Combine(invoiceFolderPath, ".staging"));
    }

    public Task<OperationResult<string>> CreateStagingPathAsync(
        InvoiceDraft draft,
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(invoiceFolderPath);
            Directory.CreateDirectory(stagingFolderPath);

            var stagingPath = Path.Combine(
                stagingFolderPath,
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

    public Task<OperationResult<string>> DiscardStagingPathAsync(string stagingPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var fullStagingPath = Path.GetFullPath(stagingPath);
            if (!IsInDirectory(fullStagingPath, stagingFolderPath))
            {
                return Task.FromResult(Failure(
                    "InvoicePdf.StagingPathOutsideStagingDirectory",
                    "The staged invoice PDF path is outside the invoice staging folder."));
            }

            if (File.Exists(fullStagingPath))
            {
                File.Delete(fullStagingPath);
            }

            return Task.FromResult(OperationResult<string>.Success(fullStagingPath));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Task.FromResult(Failure("InvoicePdf.StagingDiscardFailed", "Could not delete the staged invoice PDF."));
        }
    }

    public Task<OperationResult<string>> DiscardPublishedPathAsync(string publishedPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var fullPublishedPath = Path.GetFullPath(publishedPath);
            if (!IsInDirectory(fullPublishedPath, invoiceFolderPath) ||
                IsInDirectory(fullPublishedPath, stagingFolderPath))
            {
                return Task.FromResult(Failure(
                    "InvoicePdf.PublishedPathOutsideInvoiceDirectory",
                    "The published invoice PDF path is outside the invoice folder."));
            }

            if (File.Exists(fullPublishedPath))
            {
                File.Delete(fullPublishedPath);
            }

            return Task.FromResult(OperationResult<string>.Success(fullPublishedPath));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Task.FromResult(Failure("InvoicePdf.PublishedDiscardFailed", "Could not delete the published invoice PDF."));
        }
    }

    private static string CreateFinalFileName(InvoiceDraft draft, InvoiceNumber invoiceNumber)
    {
        var period = SanitizeFileNamePart(FormatPeriod(draft.Period));
        var studentName = SanitizeFileNamePart(draft.Student.FullName);
        return $"Invoice {invoiceNumber} {period} - {studentName}.pdf";
    }

    private static string FormatPeriod(InvoicePeriod period)
    {
        if (period.From.Year == period.To.Year && period.From.Month == period.To.Month)
        {
            var text = period.From.ToString("MMMM yyyy", DanishCulture);
            return char.ToUpper(text[0], DanishCulture) + text[1..];
        }

        return period.From.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
            + " - "
            + period.To.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }

    private static string SanitizeFileNamePart(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new StringBuilder(value.Length);
        var lastWasDash = false;
        var lastWasSpace = false;

        foreach (var character in value.Trim())
        {
            var replacement = invalidCharacters.Contains(character)
                ? '-'
                : char.IsWhiteSpace(character)
                    ? ' '
                : character;

            if (replacement == '-')
            {
                if (lastWasDash)
                {
                    continue;
                }

                lastWasDash = true;
                lastWasSpace = false;
            }
            else if (replacement == ' ')
            {
                if (lastWasSpace)
                {
                    continue;
                }

                lastWasDash = false;
                lastWasSpace = true;
            }
            else
            {
                lastWasDash = false;
                lastWasSpace = false;
            }

            builder.Append(replacement);
        }

        var sanitized = builder.ToString().Trim(' ', '-');
        return sanitized.Length == 0 ? "invoice" : sanitized;
    }

    private static bool IsInDirectory(string filePath, string directoryPath)
    {
        var normalizedFilePath = Path.GetFullPath(filePath);
        var normalizedDirectoryPath = Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return normalizedFilePath.StartsWith(normalizedDirectoryPath, StringComparison.OrdinalIgnoreCase);
    }

    private static OperationResult<string> Failure(string code, string message) =>
        OperationResult<string>.Failure(new ValidationError(code, message));
}
