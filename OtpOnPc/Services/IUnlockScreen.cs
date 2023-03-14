using OtpOnPc.Models;

using System.Threading.Tasks;

namespace OtpOnPc.Services;

public interface IUnlockScreen
{
    Task<TotpModel[]> WaitUnlocked();
}

public interface IUnlockNotifier
{
    void NotifyUnlocked(TotpModel[] items);
}

public sealed class UnlockScreen : IUnlockScreen, IUnlockNotifier
{
    private readonly TaskCompletionSource<TotpModel[]> _tcs = new();

    public void NotifyUnlocked(TotpModel[] items)
    {
        _tcs.SetResult(items);
    }

    public Task<TotpModel[]> WaitUnlocked()
    {
        return _tcs.Task;
    }
}