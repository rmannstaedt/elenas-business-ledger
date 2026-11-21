using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Infrastructure.Invoices.Pdf;

// Stages invoice PDFs beside their destination so the saved checkpoint can drive a safe move or retry.
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

    // Allocates a unique path under the invoice folder's staging directory for each PDF attempt.
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

    // Checks the PDF signature and minimum size before recording a publication checkpoint.
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

    // Computes the final filename and a checksum so recovery can identify the staged PDF after a move.
    public async Task<OperationResult<InvoicePdfPublication>> PreparePublicationAsync(
        string stagingPath, InvoiceDraft draft, InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
    {
        try
        {
            var finalPath = Path.Combine(invoiceFolderPath, CreateFinalFileName(draft, invoiceNumber));
            var checksum = await ReadChecksumAsync(stagingPath, cancellationToken);
            return OperationResult<InvoicePdfPublication>.Success(new InvoicePdfPublication(stagingPath, finalPath, checksum));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return OperationResult<InvoicePdfPublication>.Failure(
                new ValidationError("InvoicePdf.PrepareFailed", "Could not read the staged invoice PDF for publication."));
        }
    }

    // Publishes the saved PDF or verifies that the same file already occupies its destination.
    public async Task<OperationResult<string>> CompletePublicationAsync(
        InvoicePdfPublication publication, CancellationToken cancellationToken)
    {
        var (stagedPath, finalPath, checksum) = publication;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Follow the folder saved in the checkpoint, even if the configured invoice folder has changed.
            var finalDirectory = Path.GetDirectoryName(finalPath);
            if (!Path.IsPathFullyQualified(stagedPath) || !Path.IsPathFullyQualified(finalPath) ||
                finalDirectory is null ||
                !string.Equals(Path.GetDirectoryName(stagedPath), Path.Combine(finalDirectory, ".staging"),
                    StringComparison.OrdinalIgnoreCase))
            {
                return Failure("InvoicePdf.InvalidPublicationPaths", "The saved invoice PDF paths are invalid.");
            }

            if (!File.Exists(finalPath))
            {
                if (!File.Exists(stagedPath))
                {
                    return Failure("InvoicePdf.StagedFileMissing", "The saved invoice PDF is missing from both invoice folders.");
                }

                if (!string.Equals(await ReadChecksumAsync(stagedPath, cancellationToken), checksum, StringComparison.Ordinal))
                {
                    return Failure("InvoicePdf.StagedFileChanged", "The staged invoice PDF differs from the saved checkpoint.");
                }

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Move(stagedPath, finalPath, overwrite: false);
                }
                catch (IOException) when (File.Exists(finalPath))
                {
                    // Another retry may have promoted this same staged file.
                }
            }

            return string.Equals(await ReadChecksumAsync(finalPath, cancellationToken), checksum, StringComparison.Ordinal)
                ? OperationResult<string>.Success(finalPath)
                : Failure("InvoicePdf.FinalFileAlreadyExists",
                    "A different invoice PDF already occupies the saved destination. Review the existing file before retrying.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Failure("InvoicePdf.PublishFailed", "Could not publish or verify the saved invoice PDF.");
        }
    }

    private static async Task<string> ReadChecksumAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
    }

    // Removes a staged file only within the configured staging folder after an abandoned attempt.
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

    // Removes a published file in the invoice folder when the caller explicitly requests cleanup.
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
