using OtpNet;

using OtpOnPc.Models;

using Reactive.Bindings;
using Reactive.Bindings.TinyLinq;

using System;

namespace OtpOnPc.ViewModels;

public class TotpItemViewModel
{
    private ReadOnlyReactivePropertySlim<Totp> _totp;

    public TotpItemViewModel(TotpModel model)
    {
        Model.Value = model;
        Name = Model.Select(x => x.Name).ToReadOnlyReactivePropertySlim("");
        _totp = Model.Select(x => new Totp(x.SecretKey, mode: x.HashMode, totpSize: x.Size))
            .ToReadOnlyReactivePropertySlim()!;

        Model.Subscribe(_ => UpdateCode());
    }

    public ReactivePropertySlim<TotpModel> Model { get; } = new();

    public ReadOnlyReactivePropertySlim<string> Name { get; }

    public ReactivePropertySlim<string> Code { get; } = new();
    
    public ReactivePropertySlim<string> OriginalCode { get; } = new();

    public void UpdateCode()
    {
        OriginalCode.Value = _totp.Value.ComputeTotp();
        var span = OriginalCode.Value.AsSpan();
        switch (span.Length)
        {
            case 4:
                Code.Value = $"{span[..2]} {span.Slice(2, 2)}";
                break;
            case 6:
                Code.Value = $"{span[..3]} {span.Slice(3, 3)}";
                break;
            case 8:
                Code.Value = $"{span[..4]} {span.Slice(4, 4)}";
                break;
            case 9:
                Code.Value = $"{span[..3]} {span.Slice(3, 3)} {span.Slice(6, 3)}";
                break;
            case 10:
                Code.Value = $"{span[..5]} {span.Slice(5, 5)}";
                break;
        }
    }
}
