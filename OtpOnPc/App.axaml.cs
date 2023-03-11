using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using OtpOnPc.Services;
using OtpOnPc.Views;

namespace OtpOnPc;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void RegisterServices()
    {
        base.RegisterServices();
#if WINDOWS10_0_17763_0_OR_GREATER

        AvaloniaLocator.CurrentMutable.Bind<ITotpRepository>().ToSingleton<ProtectedStorageTotpRepository>();
#else
        AvaloniaLocator.CurrentMutable.Bind<ITotpRepository>().ToSingleton<DemoTotpRepository>();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}