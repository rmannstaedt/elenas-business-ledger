namespace ElenasBusinessLedger.App.ViewModels;

// Pairs a typed editor value with its UI label for use in selection lists.
public sealed record DisplayOption<T>(T Value, string DisplayName);
