using System.Diagnostics;

namespace ElenasBusinessLedger.Infrastructure.Invoices.Printing;

public sealed class ShellPrintProcessLauncher : IPrintProcessLauncher
{
    public Task PrintAsync(string pdfFilePath, string printerName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = pdfFilePath,
            Verb = "printto",
            Arguments = $"\"{printerName}\"",
            UseShellExecute = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Windows did not start a print process for the invoice PDF.");
        }

        return Task.CompletedTask;
    }
}
