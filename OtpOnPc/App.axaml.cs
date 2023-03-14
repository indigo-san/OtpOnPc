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
        var unlockScreen = new UnlockScreen();
        AvaloniaLocator.CurrentMutable
            .Bind<IUnlockScreen>().ToConstant(unlockScreen)
            .Bind<IUnlockNotifier>().ToConstant(unlockScreen);

        var settings = new SettingsService();
        AvaloniaLocator.CurrentMutable.BindToSelf(settings);

        if (settings.Settings.Value.ProtectionMode == DataProtectionMode.Windows_Security_Cryptography_DataProtection_DataProtectionProvider)
        {
#if WINDOWS10_0_17763_0_OR_GREATER
            AvaloniaLocator.CurrentMutable.Bind<ITotpRepository>().ToSingleton<ProtectedStorageTotpRepository>();
#else
            AvaloniaLocator.CurrentMutable.Bind<ITotpRepository>().ToSingleton<InMemoryTotpRepository>();
#endif
        }
        else if (settings.Settings.Value.ProtectionMode == DataProtectionMode.Aes)
        {
            AvaloniaLocator.CurrentMutable.Bind<ITotpRepository>().ToSingleton<AesTotpRepository>();
        }
        else if (settings.Settings.Value.ProtectionMode == DataProtectionMode.NoPasswordAes)
        {
            AvaloniaLocator.CurrentMutable.Bind<ITotpRepository>().ToSingleton<NoPasswordAesTotpRepository>();
        }
        else
        {
            AvaloniaLocator.CurrentMutable.Bind<ITotpRepository>().ToSingleton<InMemoryTotpRepository>();
        }

        AvaloniaLocator.CurrentMutable.BindToSelfSingleton<TotpModelManager>();
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