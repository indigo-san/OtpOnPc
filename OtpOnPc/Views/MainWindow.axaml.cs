using Avalonia;
using Avalonia.Controls;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;

using OtpOnPc.ViewModels;

using System;
using System.Linq;
using System.Reactive.Linq;

namespace OtpOnPc.Views;

public partial class MainWindow : AppWindow
{
    private readonly MainPageViewModel _mainPageViewModel = new();

    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        navigation.SelectedItem = navigation.MenuItems.OfType<object>().First();
        frame.Navigate(typeof(MainPage), _mainPageViewModel);

        navigation.ItemInvoked += OnNavigationItemInvoked;
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
    }

    public void NavigateToMainPage()
    {
        frame.Navigate(typeof(MainPage), _mainPageViewModel);
    }
}