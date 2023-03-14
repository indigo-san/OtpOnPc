using Avalonia;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;

using OtpOnPc.Services;
using OtpOnPc.ViewModels;

using System;
using System.Reactive.Linq;

using Symbol = FluentIcons.Common.Symbol;
using SymbolIcon = FluentIcons.FluentAvalonia.SymbolIcon;

namespace OtpOnPc.Views;

public partial class MainWindow : AppWindow
{
    private readonly NavigationViewItem[] _items;
    private readonly SettingsPageViewModel _settingsPageViewModel = new();
    private readonly SettingsService _settings;
    private readonly MainPageViewModel _mainPageViewModel;

    public MainWindow()
    {
        _mainPageViewModel = new MainPageViewModel();
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
        var repos = AvaloniaLocator.Current.GetRequiredService<ITotpRepository>();

        if (repos is AesTotpRepository aesRepos)
        {
            ShowUnlockScreen(aesRepos);
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
        var repos = AvaloniaLocator.Current.GetRequiredService<ITotpRepository>();
        if (repos is not AesTotpRepository)
        {
            var unlockScreen = AvaloniaLocator.Current.GetRequiredService<IUnlockNotifier>();
            unlockScreen.NotifyUnlocked(await repos.Restore());
        }

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

    public async void ShowUnlockScreen(AesTotpRepository repos)
    {
        var unlockScreen = AvaloniaLocator.Current.GetRequiredService<IUnlockScreen>();
        var viewModel = new UnlockPageViewModel(repos);
        Content = new UnlockPage
        {
            DataContext = viewModel
        };

        await unlockScreen.WaitUnlocked();

        Content = navigation;

        frame.Navigate(typeof(MainPage), _mainPageViewModel);
    }
}