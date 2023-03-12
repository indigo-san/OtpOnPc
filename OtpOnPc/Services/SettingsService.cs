using Avalonia;

using Microsoft.AspNetCore.Cryptography.KeyDerivation;

using Reactive.Bindings;

using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

#if WINDOWS10_0_17763_0_OR_GREATER
using Windows.Security.Credentials;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace OtpOnPc.Services;

public enum DataProtectionMode
{
    Windows_Security_Cryptography_DataProtection_DataProtectionProvider,
    Aes,
    GNUPrivacyGuard
}

public record AppSettings(DataProtectionMode ProtectionMode)
{
    private static readonly Lazy<AppSettings> _defaultLazy = new(() =>
    {
        var protectionMode = DefaultProtectionMode();

        return new AppSettings(protectionMode);
    });

    public static AppSettings PlatformDefault => _defaultLazy.Value;

    private static DataProtectionMode DefaultProtectionMode()
    {
        if (OperatingSystem.IsWindows())
        {
#if WINDOWS10_0_17763_0_OR_GREATER
            return DataProtectionMode.Windows_Security_Cryptography_DataProtection_DataProtectionProvider;
#else
            return DataProtectionMode.Aes;
#endif
        }

        return DataProtectionMode.Aes;
    }
}

public record AesSecret(byte[] Hash, byte[] Salt);

public class SettingsService
{
    private const string FileName = "settings.json";
    private const string SecretFileName = "secret.json";
    private readonly string _path;
    private readonly string _secretpath;
    private readonly string _directoryPath;
    internal ReactivePropertySlim<AesSecret?> _aesSecret = new();

    public SettingsService()
    {
        _directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OtpOnPc");
        _path = Path.Combine(_directoryPath, FileName);
        _secretpath = Path.Combine(_directoryPath, SecretFileName);
        if (File.Exists(_path))
        {
            try
            {
                using var stream = File.OpenRead(_path);
                Settings.Value = JsonSerializer.Deserialize<AppSettings>(stream)!;
            }
            catch
            {
            }
        }
        if (File.Exists(_secretpath))
        {
            try
            {
                using var stream = File.OpenRead(_secretpath);
                _aesSecret.Value = JsonSerializer.Deserialize<AesSecret>(stream);
            }
            catch
            {
            }
        }

        Settings.Value ??= AppSettings.PlatformDefault;

        Settings.Skip(1)
            .DistinctUntilChanged()
            .SelectMany(SaveSettings)
            .Subscribe();

        IsPasswordSet = _aesSecret.Select(x => x != null)
            .ToReadOnlyReactivePropertySlim();
    }

    private async Task<Unit> SaveSettings(AppSettings obj)
    {
        if (!Directory.Exists(_directoryPath))
        {
            Directory.CreateDirectory(_directoryPath);
        }

        using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, obj);

        return Unit.Default;
    }

    public ReactiveProperty<AppSettings> Settings { get; } = new();

    public ReadOnlyReactivePropertySlim<bool> IsPasswordSet { get; }

    public async Task<bool> UpdatePassword(string? oldPassword, string newPassword)
    {
        var result = await UpdatePasswordCore(oldPassword, newPassword);
        if (result)
        {
            // 変更された
            if (Settings.Value.ProtectionMode == DataProtectionMode.Aes)
            {
                var repos = AvaloniaLocator.Current.GetRequiredService<ITotpRepository>();
                var manager = AvaloniaLocator.Current.GetRequiredService<TotpModelManager>();
                var items = await manager.GetItems();
                await repos.Store(items, RepositoryStoreTrigger.OnUpdated);
            }
        }

        return result;
    }

    public bool CheckPassword(string password)
    {
        if (_aesSecret.Value == null)
            return false;

        byte[] oldHash = GenHash(password, _aesSecret.Value.Salt);

        return _aesSecret.Value.Hash.AsSpan().SequenceEqual(oldHash);
    }

    private async Task<bool> UpdatePasswordCore(string? oldPassword, string newPassword)
    {
        if (_aesSecret.Value != null)
        {
            if (oldPassword == null)
                return false;

            byte[] oldHash = GenHash(oldPassword, _aesSecret.Value.Salt);

            if (!_aesSecret.Value.Hash.AsSpan().SequenceEqual(oldHash))
            {
                return false;
            }
        }

        byte[] salt = new byte[128 / 8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        byte[] hash = GenHash(newPassword, salt);

        _aesSecret.Value = new AesSecret(hash, salt);

        if (!Directory.Exists(_directoryPath))
        {
            Directory.CreateDirectory(_directoryPath);
        }

        using var stream = File.Create(_secretpath);
        await JsonSerializer.SerializeAsync(stream, _aesSecret.Value);

        return true;
    }

    private static byte[] GenHash(string password, byte[] salt)
    {
        return KeyDerivation.Pbkdf2(
            password,
            salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);
    }
}
