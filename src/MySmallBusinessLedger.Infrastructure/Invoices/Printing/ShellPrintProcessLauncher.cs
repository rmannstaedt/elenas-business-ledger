using System.Diagnostics;

namespace MySmallBusinessLedger.Infrastructure.Invoices.Printing;

public sealed class ShellPrintProcessLauncher : IPrintProcessLauncher
{
    public Task PrintAsync(string pdfFilePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = pdfFilePath,
            Verb = "print",
            UseShellExecute = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        return Task.CompletedTask;
    }
}
