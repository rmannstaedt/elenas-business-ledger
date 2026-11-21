using System.Windows;

namespace ElenasBusinessLedger.App.Startup;

internal sealed record ApplicationStartupResult(Window MainWindow, IReadOnlyList<ApplicationStartupWarning> Warnings);

internal sealed record ApplicationStartupWarning(string Title, string Message);
