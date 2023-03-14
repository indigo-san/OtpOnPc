using Avalonia;

using Reactive.Bindings;

using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OtpOnPc.Services;

public enum DataProtectionMode
{
    Windows_Security_Cryptography_DataProtection_DataProtectionProvider,
    Aes,
    NoPasswordAes,
    GNUPrivacyGuard
}

public record AppSettings(DataProtectionMode ProtectionMode, bool CanResetOnUnlockScreen)
{
    private static readonly Lazy<AppSettings> _defaultLazy = new(() =>
    {
        var protectionMode = DefaultProtectionMode();

        return new AppSettings(protectionMode, true);
    });

    public static AppSettings PlatformDefault => _defaultLazy.Value;

    private static DataProtectionMode DefaultProtectionMode()
    {
#if WINDOWS10_0_17763_0_OR_GREATER
        return DataProtectionMode.Windows_Security_Cryptography_DataProtection_DataProtectionProvider;
#else
        return DataProtectionMode.NoPasswordAes;
#endif
    }
}

public class SettingsService
{
    private const string FileName = "settings.json";
    private readonly string _path;
    private readonly string _directoryPath;

    public SettingsService()
    {
        _directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OtpOnPc");
        _path = Path.Combine(_directoryPath, FileName);
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

        Settings.Value ??= AppSettings.PlatformDefault;

        Settings.Skip(1)
            .DistinctUntilChanged()
            .SelectMany(SaveSettings)
            .Subscribe();
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

    public Task Reset()
    {
        var helper = AvaloniaLocator.CurrentMutable.Bind<ITotpRepository>();

#if WINDOWS10_0_17763_0_OR_GREATER
        helper.ToSingleton<ProtectedStorageTotpRepository>();
#else
        helper.ToSingleton<NoPasswordAesTotpRepository>();
#endif
        Settings.Value = AppSettings.PlatformDefault;
        return Task.CompletedTask;
    }
}
