using Avalonia;

using Microsoft.AspNetCore.DataProtection;

using OtpNet;

using OtpOnPc.Models;
using OtpOnPc.Services;
using OtpOnPc.Views;

using Reactive.Bindings;

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace OtpOnPc.ViewModels;

public class AddAccountPageViewModel
{
    private readonly TotpModelManager _totpManager;

    private string? _keyError;

    public AddAccountPageViewModel()
    {
        _totpManager = AvaloniaLocator.Current.GetRequiredService<TotpModelManager>();
        InitialChar = Name.Select(x => x?.Length > 0 ? x[0] : default)
            .ToReadOnlyReactivePropertySlim();

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
        Step.SetValidateNotifyError(v => v is not >= 1 ? "サイズは1以上にする必要があります。" : null);
        Size.SetValidateNotifyError(v => v is not >= 1 or not <= 10 ? "サイズは1以上、10以下にする必要があります。" : null);

        IsValid = Name.ObserveHasErrors.CombineLatest(Key.ObserveHasErrors)
            .Select(x => !(x.First || x.Second))
            .ToReadOnlyReactivePropertySlim();
    }

    public ReactiveProperty<string> Name { get; }
        = new(mode: ReactivePropertyMode.IgnoreInitialValidationError | ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe);

    public ReactiveProperty<string> Key { get; }
        = new(mode: ReactivePropertyMode.IgnoreInitialValidationError | ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe);

    public ReactiveProperty<ImageIconType> IconType { get; } = new();

    public ReadOnlyReactivePropertySlim<char> InitialChar { get; }

    public ReactiveProperty<int> Step { get; } = new(30);

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
            if (key.Length == 0)
                throw new Exception();
        }
        catch
        {
            _keyError = "キーが不正です。";
            Key.ForceValidate();
            return false;
        }

        if (!IsValid.Value)
            return false;
        
        var dataProtector = AvaloniaLocator.Current.GetRequiredService<IDataProtectionProvider>().CreateProtector("SecretKey.v1");

        var model = new TotpModel(
            Guid.NewGuid(),
            dataProtector.Protect(key), 
            Name.Value,
            (OtpHashMode)HashMode.Value,
            Size.Value,
            IconType.Value,
            Step.Value);
        Random.Shared.NextBytes(key);

        await _totpManager.AddItem(model);
        return true;
    }
}
