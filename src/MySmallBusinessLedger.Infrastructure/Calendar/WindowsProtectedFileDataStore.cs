using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Google.Apis.Util.Store;

namespace MySmallBusinessLedger.Infrastructure.Calendar;

[SupportedOSPlatform("windows")]
public sealed class WindowsProtectedFileDataStore : IDataStore
{
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
        File.WriteAllBytes(GetFilePath(key), protectedBytes);
        return Task.CompletedTask;
    }

    public Task DeleteAsync<T>(string key)
    {
        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public Task<T> GetAsync<T>(string key)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return Task.FromResult(default(T)!);
        }

        var protectedBytes = File.ReadAllBytes(filePath);
        var bytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        var json = Encoding.UTF8.GetString(bytes);
        return Task.FromResult(JsonSerializer.Deserialize<T>(json)!);
    }

    public Task ClearAsync()
    {
        foreach (var file in Directory.GetFiles(directoryPath))
        {
            File.Delete(file);
        }

        return Task.CompletedTask;
    }

    private string GetFilePath(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var safeFileName = Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return Path.Combine(directoryPath, $"{safeFileName}.bin");
    }
}
