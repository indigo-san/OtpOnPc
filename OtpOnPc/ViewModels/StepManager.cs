using Reactive.Bindings;

using System.Collections.Generic;

namespace OtpOnPc.ViewModels;

public sealed class StepManager
{
    private readonly List<TotpItemViewModel> _items = new();
    private long? _prevRemain;

    public StepManager(int step)
    {
        Step = step;
    }

    public int Step { get; }

    public int Count => _items.Count;

    public ReactivePropertySlim<double> SweepAngle { get; } = new();

    public void AddItem(TotpItemViewModel item)
    {
        item.SetStepManager(this);
        _items.Add(item);
    }

    public void RemoveItem(TotpItemViewModel item)
    {
        item.SetStepManager(null);
        _items.Remove(item);
    }

    public void UpdateCode()
    {
        foreach (var item in _items)
        {
            item.UpdateCode();
        }
    }

    public void UpdateSweepAngle(long unixTime)
    {
        var remain = unixTime % (Step * 1000);
        var p = remain / (Step * 1000d);
        p = 1 - p;

        SweepAngle.Value = -(p * 360);

        if (_prevRemain.HasValue)
        {
            if (_prevRemain.Value > remain)
            {
                UpdateCode();
            }
        }
        _prevRemain = remain;
    }
}
