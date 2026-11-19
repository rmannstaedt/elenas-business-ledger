using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace MySmallBusinessLedger.App.Services;

internal static class GridWidthStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string DefaultStorePath = Path.Combine(
        AppPaths.CreateDefault().AppDataFolderPath,
        "grid-column-widths.json");

    private static readonly AsyncLocal<string?> StorePathOverrideSlot = new();

    internal static string? StorePathOverride
    {
        get => StorePathOverrideSlot.Value;
        set => StorePathOverrideSlot.Value = value;
    }

    private static string StorePath => StorePathOverride ?? DefaultStorePath;

    public static void Attach(Window window, string key, DataGrid[] grids, ColumnDefinition? pane = null)
    {
        window.SourceInitialized += (_, _) => Restore(key, grids, pane);
        window.Closing += (_, _) =>
        {
            if (window.IsLoaded)
            {
                Save(key, grids, pane);
            }
        };
    }

    private static void Restore(string key, DataGrid[] grids, ColumnDefinition? pane)
    {
        var widthsByKey = LoadWidths();
        if (pane is not null &&
            widthsByKey.TryGetValue($"{key}.{pane.Name}", out var paneWidths) &&
            paneWidths is [var paneWidth] && IsValidWidth(paneWidth))
        {
            pane.Width = new GridLength(paneWidth);
        }

        foreach (var grid in grids)
        {
            if (!widthsByKey.TryGetValue($"{key}.{grid.Name}", out var widths) || widths is null)
            {
                continue;
            }

            for (var index = 0; index < grid.Columns.Count && index < widths.Length; index++)
            {
                var column = grid.Columns[index];
                var width = widths[index];
                if (column.CanUserResize && IsValidWidth(width))
                {
                    // Older grids had at most one star column, so a saved pixel width also works as its sole weight.
                    column.Width = new DataGridLength(width, column.Width.UnitType);
                }
            }
        }
    }

    private static void Save(string key, DataGrid[] grids, ColumnDefinition? pane)
    {
        var widthsByKey = LoadWidths();
        if (pane is not null)
        {
            widthsByKey[$"{key}.{pane.Name}"] = [pane.Width.Value];
        }

        foreach (var grid in grids)
        {
            widthsByKey[$"{key}.{grid.Name}"] = grid.Columns.Select(column =>
            {
                var width = column.Width.IsStar ? column.Width.Value : column.ActualWidth;
                return IsValidWidth(width) ? width : 0;
            }).ToArray();
        }

        var storePath = StorePath;
        var temporaryPath = $"{storePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(storePath)!);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(widthsByKey, JsonOptions));
            File.Move(temporaryPath, storePath, overwrite: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning("Could not save grid widths ({0}).", error.GetType().Name);
            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception cleanupError) when (cleanupError is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning("Could not remove temporary grid widths ({0}).", cleanupError.GetType().Name);
            }
        }
    }

    private static bool IsValidWidth(double width) => double.IsFinite(width) && width > 0;

    private static Dictionary<string, double[]?> LoadWidths()
    {
        try
        {
            var widthsByKey = File.Exists(StorePath)
                ? JsonSerializer.Deserialize<Dictionary<string, double[]?>>(File.ReadAllText(StorePath)) ?? []
                : [];
            return widthsByKey
                .Where(entry => entry.Value is not null && entry.Value.All(double.IsFinite))
                .ToDictionary();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            Trace.TraceWarning("Could not load grid widths ({0}).", error.GetType().Name);
            return [];
        }
    }
}
