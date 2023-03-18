using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using OtpOnPc.Services;

using Reactive.Bindings;

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace OtpOnPc.ViewModels;

public sealed class SettingsPageViewModel : IDisposable
{
    private readonly SettingsService _settings;
    private readonly ReactivePropertySlim<ITotpRepository> _repository = new();

    public SettingsPageViewModel()
    {
        _settings = AvaloniaLocator.Current.GetRequiredService<SettingsService>();
        _repository.Value = AvaloniaLocator.Current.GetRequiredService<ITotpRepository>();
        UsingAes = _repository.Select(x => x is AesTotpRepository)
            .ToReadOnlyReactivePropertySlim();

        EncryptionMethod = _settings.Settings
            .Do(_ => _repository.Value = AvaloniaLocator.Current.GetRequiredService<ITotpRepository>())
            .Select(x => (int)x.ProtectionMode)
            .ToReactiveProperty();

        CanResetOnUnlockScreen = _settings.Settings
            .Select(x => x.CanResetOnUnlockScreen)
            .ToReactiveProperty();

        IsAesSelected = EncryptionMethod.Select(x => (DataProtectionMode)x == DataProtectionMode.Aes)
            .ToReadOnlyReactivePropertySlim();
        SelectedAndNotUsedAes = UsingAes.CombineLatest(EncryptionMethod)
            .Select(x => !x.First && (DataProtectionMode)x.Second == DataProtectionMode.Aes)
            .ToReadOnlyReactivePropertySlim();

        OldPassword = new ReactiveProperty<string>();
        NewPassword = new ReactiveProperty<string>();

        Save = new AsyncReactiveCommand();
        Save.Subscribe(async () =>
        {
            ErrorMessage.Value = "";
            if (await UpdateEncryptionMethod())
            {
                await UpdatePassword();
            }
        });
    }

    public ReactiveProperty<int> EncryptionMethod { get; }

    public ReactiveProperty<string> OldPassword { get; }

    public ReactiveProperty<string> NewPassword { get; }

    public ReactiveProperty<bool> CanResetOnUnlockScreen { get; }

    public ReadOnlyReactivePropertySlim<bool> IsAesSelected { get; }

    public ReadOnlyReactivePropertySlim<bool> SelectedAndNotUsedAes { get; }

    public ReadOnlyReactivePropertySlim<bool> UsingAes { get; }

    public AsyncReactiveCommand Save { get; }

    public ReactivePropertySlim<string> ErrorMessage { get; } = new();

#pragma warning disable CA1822
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

    public void Dispose()
    {
        UsingAes.Dispose();
    }

    private async ValueTask<bool> UpdateEncryptionMethod()
    {
        var settings = _settings.Settings.Value;
        var mode = (DataProtectionMode)EncryptionMethod.Value;
        if (mode != settings.ProtectionMode)
        {
            var oldrepos = AvaloniaLocator.Current.GetRequiredService<ITotpRepository>();
            ITotpRepository? newrepos = null;
            bool result = false;
            switch (mode)
            {
                case DataProtectionMode.Windows_Security_Cryptography_DataProtection_DataProtectionProvider:
#if WINDOWS10_0_17763_0_OR_GREATER
                    newrepos = new ProtectedStorageTotpRepository();
                    result = true;
#endif
                    break;
                case DataProtectionMode.Aes:
                    if (string.IsNullOrWhiteSpace(NewPassword.Value))
                    {
                        ErrorMessage.Value = "パスワードを指定してください。";
                        return false;
                    }

                    newrepos = new AesTotpRepository();
                    await ((AesTotpRepository)newrepos).UpdatePassword(null, NewPassword.Value);
                    NewPassword.Value = "";
                    result = true;
                    break;
                case DataProtectionMode.NoPasswordAes:
                    newrepos = new NoPasswordAesTotpRepository();
                    result = true;
                    break;
                case DataProtectionMode.GNUPrivacyGuard:
                    break;
                default:
                    break;
            }

            if (result && newrepos != null)
            {
                try
                {
                    var manager = AvaloniaLocator.Current.GetRequiredService<TotpModelManager>();
                    var items = await manager.GetItems();
                    await newrepos.Store(items, RepositoryStoreTrigger.OnAdded);
                    await oldrepos.Clear();

                    AvaloniaLocator.CurrentMutable.BindToSelf(newrepos);
                    _repository.Value = newrepos;
                    _settings.Settings.Value = settings with
                    {
                        ProtectionMode = mode
                    };
                }
                catch (Exception ex)
                {
                    if (await ExceptionDialog.Handle(ex) == ExceptionDialogResult.Shutdown)
                    {
                        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                        lifetime?.Shutdown((int)ExitCodes.FailedToChangeRepository);
                    }

                    return false;
                }
            }
        }

        return true;
    }

    private async ValueTask UpdatePassword()
    {
        if (!(string.IsNullOrWhiteSpace(OldPassword.Value)
            || string.IsNullOrWhiteSpace(NewPassword.Value)))
        {
            var repos = AvaloniaLocator.Current.GetRequiredService<ITotpRepository>();
            if (repos is AesTotpRepository aesRepos)
            {
                if (await aesRepos.UpdatePassword(OldPassword.Value, NewPassword.Value))
                {
                    OldPassword.Value = "";
                    NewPassword.Value = "";
                }
                else
                {
                    ErrorMessage.Value = "古いパスワードが違います";
                }
            }
        }
    }
}
