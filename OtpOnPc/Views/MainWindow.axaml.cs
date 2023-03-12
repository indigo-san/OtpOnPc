using Avalonia;
using Avalonia.Controls;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;

using OtpOnPc.ViewModels;

using System;
using System.Linq;
using System.Reactive.Linq;

using SymbolIcon = FluentIcons.FluentAvalonia.SymbolIcon;
using Symbol = FluentIcons.Common.Symbol;
using OtpOnPc.Services;
using System.Threading.Tasks;

namespace OtpOnPc.Views;

public partial class MainWindow : AppWindow
{
    private readonly NavigationViewItem[] _items;
    private readonly MainPageViewModel _mainPageViewModel = new();
    private readonly SettingsPageViewModel _settingsPageViewModel = new();
    private readonly SettingsService _settings;

    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        _items = new NavigationViewItem[]
        {
            new()
            {
                Content = "一覧",
                Tag = "Main",
                Icon = new SymbolIcon
                {
                    Symbol= Symbol.Password
                }
            },
            new()
            {
                Content = "アカウントを追加",
                Tag = "Add",
                Icon = new SymbolIcon
                {
                    Symbol= Symbol.PersonAdd
                }
            }
        };
        navigation.MenuItems = _items;
        navigation.SelectedItem = _items[0];
        _settings = AvaloniaLocator.Current.GetRequiredService<SettingsService>();
        
        if (_settings.Settings.Value.ProtectionMode == DataProtectionMode.Aes)
        {
            if (_settings.IsPasswordSet.Value)
            {
                ShowSignInScreen(s => Task.FromResult(_settings.CheckPassword(s)));
            }
        }
        else
        {
            frame.Navigate(typeof(MainPage), _mainPageViewModel);
        }

        navigation.ItemInvoked += OnNavigationItemInvoked;
    }

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        await _mainPageViewModel._initializeTask;
    }

    private void OnNavigationItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        switch (e.InvokedItemContainer.Tag)
        {
            case "Main":
                frame.Navigate(typeof(MainPage), _mainPageViewModel, e.RecommendedNavigationTransitionInfo);
                break;
            case "Add":
                frame.Navigate(typeof(AddAccountPage), new AddAccountPageViewModel(), e.RecommendedNavigationTransitionInfo);
                break;
            default:
                break;
        }

        if (e.IsSettingsInvoked)
        {
            frame.Navigate(typeof(SettingsPage), _settingsPageViewModel, e.RecommendedNavigationTransitionInfo);
        }
    }

    public void NavigateToMainPage()
    {
        frame.Navigate(typeof(MainPage), _mainPageViewModel);
        navigation.SelectedItem = _items[0];
    }

    public void ShowSignInScreen(Func<string, Task<bool>> verify)
    {
        var viewModel = new SignInPageViewModel(verify);
        Content = new SignInPage
        {
            DataContext = viewModel
        };

        viewModel.SignedIn += (_, _) => Content = navigation;
    }
}