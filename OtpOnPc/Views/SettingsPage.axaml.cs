using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;

using OtpOnPc.ViewModels;

using System;

namespace OtpOnPc.Views;

public partial class SettingsPage : UserControl
{
    private IDisposable? _disposable;

    public SettingsPage()
    {
        Resources["SettingsPage_TextBox_Width"] = double.NaN;
        Resources["SettingsPage_TextBox_HorizontalAlignment"] = HorizontalAlignment.Stretch;
        InitializeComponent();
        AddHandler(Frame.NavigatedToEvent, OnNavigatedTo, RoutingStrategies.Direct);
    }

    private async void ResetClick(object? sender, RoutedEventArgs e)
    {
        await new ResetDialog(VisualRoot).ShowAsync();
    }

    private void OnNavigatedTo(object? sender, NavigationEventArgs e)
    {
        if (e.Parameter is SettingsPageViewModel viewModel)
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
                        Resources["SettingsPage_TextBox_Width"] = 300d;
                        Resources["SettingsPage_TextBox_HorizontalAlignment"] = HorizontalAlignment.Left;
                    }
                    else
                    {
                        title.Margin = new(40, 0, 0, 0);
                        Resources["SettingsPage_TextBox_Width"] = double.NaN;
                        Resources["SettingsPage_TextBox_HorizontalAlignment"] = HorizontalAlignment.Stretch;
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
}
