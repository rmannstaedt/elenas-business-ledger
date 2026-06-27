using System.IO;
using System.Text.Json;
using System.Windows.Controls;

namespace MySmallBusinessLedger.App.Services;

public static class DataGridColumnWidthStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string DefaultStorePath = Path.Combine(
        AppPaths.CreateDefault().AppDataFolderPath,
        "grid-column-widths.json");

    internal static string? StorePathOverride { get; set; }

    private static string StorePath => StorePathOverride ?? DefaultStorePath;

    public static void Attach(DataGrid grid, string key)
    {
        grid.Loaded += (_, _) => Restore(grid, key);
        grid.Unloaded += (_, _) => Save(grid, key);
    }

    private static void Restore(DataGrid grid, string key)
    {
        var widthsByKey = LoadWidths();
        if (!widthsByKey.TryGetValue(key, out var widths))
        {
            return;
        }

        for (var index = 0; index < grid.Columns.Count && index < widths.Length; index++)
        {
            var width = widths[index];
            if (double.IsFinite(width) && width > 20)
            {
                grid.Columns[index].Width = new DataGridLength(width);
            }
        }
    }

    private static void Save(DataGrid grid, string key)
    {
        if (grid.Columns.Count == 0)
        {
            return;
        }

        var widths = grid.Columns
            .Select(column => column.ActualWidth)
            .Where(double.IsFinite)
            .ToArray();
        if (widths.Length == 0)
        {
            return;
        }

        var widthsByKey = LoadWidths();
        widthsByKey[key] = widths;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
            File.WriteAllText(StorePath, JsonSerializer.Serialize(widthsByKey, JsonOptions));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static Dictionary<string, double[]> LoadWidths()
    {
        if (!File.Exists(StorePath))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, double[]>>(File.ReadAllText(StorePath)) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }
}
