using Avalonia;

using OtpOnPc.Services;

using Reactive.Bindings;

using System;
using System.Reactive.Linq;

namespace OtpOnPc.ViewModels;

public class SettingsPageViewModel
{
    private readonly SettingsService _settings;

    public SettingsPageViewModel()
    {
        _settings = AvaloniaLocator.Current.GetRequiredService<SettingsService>();
        IsPasswordSet = _settings.IsPasswordSet;

        EncryptionMethod = _settings.Settings.Select(x => (int)x.ProtectionMode).ToReactiveProperty();

        EncryptionMethod.Skip(1)
            .DistinctUntilChanged()
            .Where(x => x is >= 0 and < 3)
            .Select(v => (DataProtectionMode)v)
            .Subscribe(v => _settings.Settings.Value = _settings.Settings.Value with { ProtectionMode = v });

        OldPassword = new ReactiveProperty<string>();
        NewPassword = new ReactiveProperty<string>();
        UpdatePassword = new AsyncReactiveCommand()
            .WithSubscribe(async () =>
            {
                if (await _settings.UpdatePassword(OldPassword.Value, NewPassword.Value))
                {
                    OldPassword.Value = "";
                    NewPassword.Value = "";
                }
            });
    }

    public ReactiveProperty<int> EncryptionMethod { get; }

    public ReactiveProperty<string> OldPassword { get; }

    public ReactiveProperty<string> NewPassword { get; }

    public AsyncReactiveCommand UpdatePassword { get; }

    public ReadOnlyReactivePropertySlim<bool> IsPasswordSet { get; }

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
}
