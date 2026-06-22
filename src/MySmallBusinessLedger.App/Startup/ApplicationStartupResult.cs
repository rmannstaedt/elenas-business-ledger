using System.Windows;

namespace MySmallBusinessLedger.App.Startup;

internal sealed record ApplicationStartupResult(Window MainWindow, IReadOnlyList<ApplicationStartupWarning> Warnings);

internal sealed record ApplicationStartupWarning(string Title, string Message);
