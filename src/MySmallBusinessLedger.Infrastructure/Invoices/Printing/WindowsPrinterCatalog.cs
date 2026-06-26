using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MySmallBusinessLedger.Infrastructure.Invoices.Printing;

public sealed class WindowsPrinterCatalog : IPrinterCatalog
{
    private const int PrinterEnumLocal = 0x00000002;
    private const int PrinterEnumConnections = 0x00000004;
    private const int PrinterInfoLevel = 4;

    public Task<IReadOnlyList<string>> ListPrinterNamesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var flags = PrinterEnumLocal | PrinterEnumConnections;
        _ = EnumPrinters(flags, null, PrinterInfoLevel, IntPtr.Zero, 0, out var bytesNeeded, out _);
        if (bytesNeeded == 0)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var buffer = Marshal.AllocHGlobal(bytesNeeded);
        try
        {
            if (!EnumPrinters(flags, null, PrinterInfoLevel, buffer, bytesNeeded, out _, out var printersReturned))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var itemSize = Marshal.SizeOf<PrinterInfo4>();
            var printers = Enumerable.Range(0, printersReturned)
                .Select(index => Marshal.PtrToStructure<PrinterInfo4>(buffer + (index * itemSize)).PrinterName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();

            return Task.FromResult<IReadOnlyList<string>>(printers);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool EnumPrinters(
        int flags,
        string? name,
        int level,
        IntPtr printerEnum,
        int bufferSize,
        out int bytesNeeded,
        out int printersReturned);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PrinterInfo4
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? PrinterName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? ServerName;

        public uint Attributes;
    }
}
