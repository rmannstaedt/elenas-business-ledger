using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Google.Apis.Util.Store;

namespace ElenasBusinessLedger.Infrastructure.Calendar;

// Persists OAuth credentials encrypted for the current Windows user so later refreshes can reuse authorization.
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

    // Encrypts the Google credential for the current Windows user and writes it under the hashed key.
    public Task StoreAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        var bytes = Encoding.UTF8.GetBytes(json);
        var protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(GetHashedFilePath(key), protectedBytes);
        DeleteFileIfExists(GetLegacyFilePath(key));
        return Task.CompletedTask;
    }

    // Removes both filename variants so a deleted credential cannot be loaded through the legacy path.
    public Task DeleteAsync<T>(string key)
    {
        DeleteFileIfExists(GetHashedFilePath(key));
        DeleteFileIfExists(GetLegacyFilePath(key));

        return Task.CompletedTask;
    }

    // Reads a saved credential and migrates its legacy filename when it is first accessed.
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

    // Checks whether a token already exists without creating its directory just to report Settings status.
    public static bool ContainsStoredKey(string directoryPath, string key) =>
        Directory.Exists(directoryPath) && new WindowsProtectedFileDataStore(directoryPath).ContainsKey(key);

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

    // Clears the credential store so Google sign-in can start again without saved tokens.
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
