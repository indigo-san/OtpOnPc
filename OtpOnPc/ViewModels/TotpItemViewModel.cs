using Avalonia.Controls.Mixins;

using OtpNet;

using OtpOnPc.Models;

using Reactive.Bindings;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace OtpOnPc.ViewModels;

public sealed class TotpItemViewModel : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ReadOnlyReactivePropertySlim<Totp> _totp;
    private readonly ReactivePropertySlim<StepManager?> _stepManager = new();

    public TotpItemViewModel(TotpModel model)
    {
        Model.Value = model;
        Name = Model.Select(x => x.Name)
            .ToReadOnlyReactivePropertySlim("")
            .DisposeWith(_disposables);
        _totp = Model.Select(x => new Totp(x.SecretKey, mode: x.HashMode, totpSize: x.Size))
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables)!;

        Model.Subscribe(_ => UpdateCode())
            .DisposeWith(_disposables);

        Model.Select(x => x.Step)
            .CombineWithPrevious()
            .Skip(1)
            .Subscribe(t => StepChanged?.Invoke(this, (t.OldValue, t.NewValue)))
            .DisposeWith(_disposables);

        SweepAngle = _stepManager
            .Select(x => x?.SweepAngle ?? Observable.Return(0d))
            .Switch()
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables);
    }

    public event EventHandler<(int OldStep, int NewStep)>? StepChanged;

    public ReactivePropertySlim<TotpModel> Model { get; } = new();

    public ReadOnlyReactivePropertySlim<string> Name { get; }

    public ReactivePropertySlim<string> Code { get; } = new();

    public ReactivePropertySlim<string> OriginalCode { get; } = new();

    public ReadOnlyReactivePropertySlim<double> SweepAngle { get; }

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

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public void SetStepManager(StepManager? stepManager)
    {
        _stepManager.Value = stepManager;
    }
}
