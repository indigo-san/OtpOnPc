using Avalonia;

using OtpOnPc.Services;

using Reactive.Bindings;
using Reactive.Bindings.TinyLinq;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace OtpOnPc.ViewModels;

public class UnlockPageViewModel
{
    private readonly AesTotpRepository _repos;
    private readonly IUnlockNotifier _unlockNotifier;

    public UnlockPageViewModel(AesTotpRepository repos)
    {
        _unlockNotifier = AvaloniaLocator.Current.GetRequiredService<IUnlockNotifier>();
        var settings = AvaloniaLocator.Current.GetRequiredService<SettingsService>();

        Unlock = new AsyncReactiveCommand()
            .WithSubscribe(UnlockCore);
        CanReset = settings.Settings
            .Select(x => x.CanResetOnUnlockScreen)
            .ToReadOnlyReactivePropertySlim();

        _repos = repos;
    }

    public ReactiveProperty<string> Password { get; } = new();

    public ReactivePropertySlim<string> ErrorMessage { get; } = new();

    public ReactivePropertySlim<bool> Varifying { get; } = new(false);

    public AsyncReactiveCommand Unlock { get; }

    public ReadOnlyReactivePropertySlim<bool> CanReset { get; }

#pragma warning disable CA1822
    // Windows.Security.Cryptography.DataProtection
    public bool IsWSCDEnabled
    {
        get
        {
#if WINDOWS10_0_17763_0_OR_GREATER
            return true;
#else
            return false;
#endif
        }
    }
#pragma warning restore CA1822

    private async Task UnlockCore()
    {
        try
        {
            Varifying.Value = true;
            var items = await _repos.Unlock(Password.Value);
            _unlockNotifier.NotifyUnlocked(items);
        }
        catch
        {
            ErrorMessage.Value = """
                復元できませんでした。
                パスワードが違う可能性があります。
                """;
        }
        finally
        {
            Varifying.Value = false;
        }
    }

    public async Task UseNoPasswordAes()
    {
        try
        {
            var settings = AvaloniaLocator.Current.GetRequiredService<SettingsService>();
            var repos = new NoPasswordAesTotpRepository();
            var items = await repos.Restore();

            // この時点で復元が成功しているので設定を変更する。
            AvaloniaLocator.CurrentMutable.Bind<ITotpRepository>().ToConstant(repos);
            settings.Settings.Value = settings.Settings.Value with
            {
                ProtectionMode = DataProtectionMode.NoPasswordAes
            };

            _unlockNotifier.NotifyUnlocked(items);
        }
        catch
        {
            ErrorMessage.Value = "復元できませんでした。";
        }
    }

    public
#if WINDOWS10_0_17763_0_OR_GREATER
        async
#endif
        Task UseWSCD()
    {
#if WINDOWS10_0_17763_0_OR_GREATER
        try
        {
            var settings = AvaloniaLocator.Current.GetRequiredService<SettingsService>();
            var repos = new ProtectedStorageTotpRepository();
            var items = await repos.Restore();

            AvaloniaLocator.CurrentMutable.Bind<ITotpRepository>().ToConstant(repos);
            settings.Settings.Value = settings.Settings.Value with
            {
                ProtectionMode = DataProtectionMode.Windows_Security_Cryptography_DataProtection_DataProtectionProvider
            };

            _unlockNotifier.NotifyUnlocked(items);
        }
        catch
        {
            ErrorMessage.Value = "復元できませんでした。";
        }
#else
        return Task.CompletedTask;
#endif
    }
}