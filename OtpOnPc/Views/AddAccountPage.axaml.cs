using System;

using Avalonia;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;

using OtpOnPc.ViewModels;

namespace OtpOnPc.Views;

public partial class AddAccountPage : UserControl
{
    private IDisposable? _disposable;

    public AddAccountPage()
    {
        Resources["AddAccount_TextBox_Width"] = double.NaN;
        Resources["AddAccount_TextBox_HorizontalAlignment"] = HorizontalAlignment.Stretch;
        InitializeComponent();
        AddHandler(Frame.NavigatedToEvent, OnNavigatedTo, RoutingStrategies.Direct);
    }

    private void OnNavigatedTo(object? sender, NavigationEventArgs e)
    {
        if (e.Parameter is AddAccountPageViewModel viewModel)
        {
            DataContext = viewModel;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var nav = this.FindAncestorOfType<NavigationView>();
        if (nav != null)
        {
            _disposable = nav.GetObservable(NavigationView.DisplayModeProperty)
                .Subscribe(mode =>
                {
                    if (mode == NavigationViewDisplayMode.Expanded)
                    {
                        title.Margin = new(4, 0, 0, 0);
                        Resources["AddAccount_TextBox_Width"] = 300d;
                        Resources["AddAccount_TextBox_HorizontalAlignment"] = HorizontalAlignment.Left;
                    }
                    else
                    {
                        title.Margin = new(40, 0, 0, 0);
                        Resources["AddAccount_TextBox_Width"] = double.NaN;
                        Resources["AddAccount_TextBox_HorizontalAlignment"] = HorizontalAlignment.Stretch;
                    }
                });
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _disposable?.Dispose();
        DataContext = null;
    }

    private async void AddClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddAccountPageViewModel viewModel)
        {
            if (await viewModel.Add())
            {
                this.FindAncestorOfType<MainWindow>()?.NavigateToMainPage();
            }
        }
    }
}
