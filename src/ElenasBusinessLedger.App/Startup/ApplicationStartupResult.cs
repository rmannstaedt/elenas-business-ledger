using System.Windows;

namespace ElenasBusinessLedger.App.Startup;

// Returns the constructed main window and warnings so App can display both after initialization.
internal sealed record ApplicationStartupResult(Window MainWindow, IReadOnlyList<ApplicationStartupWarning> Warnings);

// Carries the title and explanation of a startup issue that can be shown to the user.
internal sealed record ApplicationStartupWarning(string Title, string Message);
