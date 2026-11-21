using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Google.Apis.Util.Store;

namespace ElenasBusinessLedger.Infrastructure.Calendar;

[SupportedOSPlatform("windows")]
public sealed class WindowsProtectedFileDataStore : IDataStore
{
    // Existing token files require the original DPAPI entropy after a product rename.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MySmallBusinessLedger.GoogleCalendarOAuth");
    private readonly string directoryPath;

    public WindowsProtectedFileDataStore(string directoryPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows DPAPI token storage requires Windows.");
        }

        this.directoryPath = directoryPath;
        Directory.CreateDirectory(directoryPath);
    }

    public Task StoreAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        var bytes = Encoding.UTF8.GetBytes(json);
        var protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(GetHashedFilePath(key), protectedBytes);
        DeleteFileIfExists(GetLegacyFilePath(key));
        return Task.CompletedTask;
    }

    public Task DeleteAsync<T>(string key)
    {
        DeleteFileIfExists(GetHashedFilePath(key));
        DeleteFileIfExists(GetLegacyFilePath(key));

        return Task.CompletedTask;
    }

    public Task<T> GetAsync<T>(string key)
    {
        var filePath = GetHashedFilePath(key);
        if (!File.Exists(filePath))
        {
            var legacyFilePath = GetLegacyFilePath(key);
            if (!File.Exists(legacyFilePath))
            {
                return Task.FromResult(default(T)!);
            }

            var legacyValue = Read<T>(legacyFilePath);
            File.Copy(legacyFilePath, filePath, overwrite: false);
            File.Delete(legacyFilePath);
            return Task.FromResult(legacyValue);
        }

        return Task.FromResult(Read<T>(filePath));
    }

    public bool ContainsKey(string key) =>
        File.Exists(GetHashedFilePath(key)) ||
        File.Exists(GetLegacyFilePath(key));

    private static T Read<T>(string filePath)
    {
        var protectedBytes = File.ReadAllBytes(filePath);
        var bytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        var json = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    public Task ClearAsync()
    {
        foreach (var file in Directory.GetFiles(directoryPath))
        {
            File.Delete(file);
        }

        return Task.CompletedTask;
    }

    private string GetHashedFilePath(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var safeFileName = Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return Path.Combine(directoryPath, $"{safeFileName}.bin");
    }

    private string GetLegacyFilePath(string key)
    {
        var safeFileName = Uri.EscapeDataString(key).Replace("%", "_");
        return Path.Combine(directoryPath, $"{safeFileName}.bin");
    }

    private static void DeleteFileIfExists(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
