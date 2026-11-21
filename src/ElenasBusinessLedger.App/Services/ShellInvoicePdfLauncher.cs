using System.Diagnostics;

namespace ElenasBusinessLedger.App.Services;

public sealed class ShellInvoicePdfLauncher : IInvoicePdfLauncher
{
    public Task OpenAsync(string pdfFilePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Process.Start(new ProcessStartInfo
        {
            FileName = pdfFilePath,
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }
}
