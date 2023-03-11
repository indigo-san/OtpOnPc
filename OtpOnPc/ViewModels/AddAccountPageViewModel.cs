using Avalonia;

using OtpNet;

using OtpOnPc.Models;
using OtpOnPc.Services;

using Reactive.Bindings;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtpOnPc.ViewModels;

public class AddAccountPageViewModel
{
    private readonly ITotpRepository _totpRepos;

    private string? _keyError;

    public AddAccountPageViewModel()
    {
        _totpRepos = AvaloniaLocator.Current.GetRequiredService<ITotpRepository>();
        Name.SetValidateNotifyError(v => string.IsNullOrWhiteSpace(v) ? "名前を空白にすることはできません。" : null);
        Key.SetValidateNotifyError(v =>
        {
            if (string.IsNullOrWhiteSpace(v))
            {
                return "キーを空白にすることはできません。";
            }
            else if (_keyError != null)
            {
                var str = _keyError;
                _keyError = null;
                return str;
            }
            else
            {
                return null;
            }
        });
        Size.SetValidateNotifyError(v => v is not >= 1 or not <= 10 ? "サイズは1以上、10以下にする必要があります。" : null);

        IsValid = Name.ObserveHasErrors.CombineLatest(Key.ObserveHasErrors)
            .Select(x => !(x.First || x.Second))
            .ToReadOnlyReactivePropertySlim();
    }

    public ReactiveProperty<string> Name { get; }
        = new(mode: ReactivePropertyMode.IgnoreInitialValidationError | ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe);

    public ReactiveProperty<string> Key { get; }
        = new(mode: ReactivePropertyMode.IgnoreInitialValidationError | ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe);

    public ReactiveProperty<int> HashMode { get; } = new();

    public ReactiveProperty<int> Size { get; } = new(6);

    public ReadOnlyReactivePropertySlim<bool> IsValid { get; }

    public async Task<bool> Add()
    {
        Name.ForceValidate();
        Key.ForceValidate();
        byte[]? key;

        try
        {
            key = Base32Encoding.ToBytes(Key.Value);
        }
        catch
        {
            _keyError = "キーが不正です。";
            Key.ForceValidate();
            return false;
        }

        await _totpRepos.AddItem(new TotpModel(Guid.NewGuid(), key, Name.Value, (OtpHashMode)HashMode.Value, Size.Value));
        return true;
    }
}
