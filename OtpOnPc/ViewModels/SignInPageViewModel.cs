using Reactive.Bindings;
using Reactive.Bindings.TinyLinq;

using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace OtpOnPc.ViewModels;

public class SignInPageViewModel
{
    private Func<string, Task<bool>> _verify;

    public SignInPageViewModel(Func<string, Task<bool>> verify)
    {
        _verify = verify;
        Unlock = new AsyncReactiveCommand()
            .WithSubscribe(UnlockCore);
    }

    public event EventHandler? SignedIn;

    public ReactiveProperty<string> Password { get; } = new();

    public ReactivePropertySlim<bool> Varifying { get; } = new(false);

    public AsyncReactiveCommand Unlock { get; }

    private async Task UnlockCore()
    {
        try
        {
            Varifying.Value = true;
            if (await _verify(Password.Value))
            {
                SignedIn?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            Varifying.Value = false;
        }
    }
}