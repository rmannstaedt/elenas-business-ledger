using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace ElenasBusinessLedger.App.Services;

public static class WindowPlacementStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string DefaultStorePath = AppPaths.CreateDefault().WindowPlacementPath;
    private static readonly AsyncLocal<string?> StorePathOverrideSlot = new();

    internal static string? StorePathOverride
    {
        get => StorePathOverrideSlot.Value;
        set => StorePathOverrideSlot.Value = value;
    }

    private static string StorePath => StorePathOverride ?? DefaultStorePath;

    public static void Attach(Window window, string key, bool rememberSize = true)
    {
        window.SourceInitialized += (_, _) => Restore(window, key, rememberSize);
        window.Closing += (_, _) => Save(window, key);
    }

    private static void Restore(Window window, string key, bool rememberSize)
    {
        var placements = LoadPlacements();
        if (!placements.TryGetValue(key, out var placement))
        {
            return;
        }

        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualWidth = SystemParameters.VirtualScreenWidth;
        var virtualHeight = SystemParameters.VirtualScreenHeight;

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        if (rememberSize)
        {
            window.Width = Math.Clamp(placement.Width, window.MinWidth, virtualWidth);
            window.Height = Math.Clamp(placement.Height, window.MinHeight, virtualHeight);
        }

        window.Left = Math.Clamp(placement.Left, virtualLeft, virtualLeft + virtualWidth - window.Width);
        window.Top = Math.Clamp(placement.Top, virtualTop, virtualTop + virtualHeight - window.Height);
    }

    private static void Save(Window window, string key)
    {
        if (window.WindowState != WindowState.Normal)
        {
            return;
        }

        if (!double.IsFinite(window.Left) ||
            !double.IsFinite(window.Top) ||
            !double.IsFinite(window.Width) ||
            !double.IsFinite(window.Height))
        {
            return;
        }

        var placements = LoadPlacements();
        placements[key] = new WindowPlacement(window.Left, window.Top, window.Width, window.Height);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
            File.WriteAllText(StorePath, JsonSerializer.Serialize(placements, JsonOptions));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static Dictionary<string, WindowPlacement> LoadPlacements()
    {
        if (!File.Exists(StorePath))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, WindowPlacement>>(File.ReadAllText(StorePath)) ?? [];
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

    private sealed record WindowPlacement(double Left, double Top, double Width, double Height);
}
