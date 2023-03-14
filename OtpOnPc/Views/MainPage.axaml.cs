using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Rendering;
using Avalonia.Xaml.Interactivity;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;

using OtpOnPc.ViewModels;

using System;

#if WINDOWS10_0_17763_0_OR_GREATER
using Windows.Security.Credentials.UI;
#endif

namespace OtpOnPc.Views;

public partial class MainPage : UserControl
{
    public static readonly StyledProperty<double> SweepAngleProperty = Sector.SweepAngleProperty.AddOwner<MainPage>();
    private readonly UiThreadRenderTimer _renderTimer;
    private readonly FAMenuFlyout _menuFlyout;
    private long? _prevRemain;

    public MainPage()
    {
        InitializeComponent();
        _renderTimer = new UiThreadRenderTimer(60);
        AddHandler(Frame.NavigatedToEvent, OnNavigatedTo, RoutingStrategies.Direct);
        _menuFlyout = new FAMenuFlyout();
        var copyMenuItem = new MenuFlyoutItem
        {
            Text = "�R�s�[",
            InputGesture = new KeyGesture(Key.C, KeyModifiers.Control),
            Icon = new SymbolIcon
            {
                Symbol = Symbol.Copy
            }
        };
        copyMenuItem.Click += CopyMenuItem_Click;
        var deleteMenuItem = new MenuFlyoutItem
        {
            Text = "�폜",
            Icon = new SymbolIcon
            {
                Symbol = Symbol.Delete
            }
        };
        deleteMenuItem.Click += DeleteMenuItem_Click;
        _menuFlyout.Items = new MenuFlyoutItem[] { copyMenuItem, deleteMenuItem };

        listBox.ItemContainerGenerator.Materialized += OnItemContainerGeneratorMaterialized;
        listBox.ItemContainerGenerator.Dematerialized += OnItemContainerGeneratorDematerialized;
    }

    public double SweepAngle
    {
        get => GetValue(SweepAngleProperty);
        set => SetValue(SweepAngleProperty, value);
    }

    private void CopyMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as MenuFlyoutItem)?.FindLogicalAncestorOfType<ListBoxItem>()?.DataContext is TotpItemViewModel itemViewModel
            && DataContext is MainPageViewModel viewModel)
        {
            viewModel.CopySelected.Execute(itemViewModel);
        }
    }

    private async void DeleteMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as MenuFlyoutItem)?.FindLogicalAncestorOfType<ListBoxItem>()?.DataContext is TotpItemViewModel itemViewModel
            && DataContext is MainPageViewModel viewModel)
        {
            var dialog = new TaskDialog
            {
                XamlRoot = VisualRoot,
                Title = "�m�F",
                Header = "���̃A�J�E���g���폜���܂��B",
                Content = $"""
                    {itemViewModel.Name.Value}�v���폜���܂��B
                    ���̑�������s����ƔF�؃R�[�h�̐����͎~�܂�܂����A
                    �A�J�E���g�̓�i�K�F�؂͖����ɂȂ�܂���B
                    ��������s����O�ɃA�J�E���g�̓�i�K�F�؂�
                    ����������Ă��邱�Ƃ��m�F���Ă��������B
                    """,
                Buttons =
                {
                    new TaskDialogButton("�폜����", true),
                    new TaskDialogButton("�폜���Ȃ�", false)
                }
            };

            if ((await dialog.ShowAsync()) is true)
            {
#if WINDOWS10_0_17763_0_OR_GREATER
                if (await UserConsentVerifier.RequestVerificationAsync($"�u{itemViewModel.Name.Value}�v���폜���܂��B") == UserConsentVerificationResult.Verified)
                {
                    await viewModel.DeleteItem(itemViewModel);
                }
#else
                await viewModel.DeleteItem(itemViewModel);
#endif
            }
        }
    }

    private void OnItemContainerGeneratorDematerialized(object? sender, ItemContainerEventArgs e)
    {
        foreach (var item in e.Containers)
        {
            if (item.ContainerControl is ListBoxItem listBoxItem)
            {
                Interaction.GetBehaviors(listBoxItem).Clear();

                listBoxItem.ContextFlyout = null;
            }
        }
    }

    private void OnItemContainerGeneratorMaterialized(object? sender, ItemContainerEventArgs e)
    {
        foreach (var item in e.Containers)
        {
            if (item.ContainerControl is ListBoxItem listBoxItem)
            {
                var list = Interaction.GetBehaviors(listBoxItem);
                list.Add(new ListBoxItemBehavior());

                listBoxItem.ContextFlyout = _menuFlyout;
            }
        }
    }

    private void OnRenderTimerTick(TimeSpan obj)
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var remain = unixTime % 30000;
        var p = remain / 30000d;
        p = 1 - p;

        SweepAngle = -(p * 360);

        if (_prevRemain.HasValue)
        {
            if (_prevRemain.Value > remain)
            {
                if (DataContext is MainPageViewModel viewModel)
                {
                    viewModel.UpdateCode();
                }
            }
        }
        _prevRemain = remain;
    }

    private void OnNavigatedTo(object? sender, NavigationEventArgs e)
    {
        if (e.Parameter is MainPageViewModel viewModel)
        {
            viewModel.UpdateCode();
            DataContext = viewModel;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _renderTimer.Tick += OnRenderTimerTick;
        if (DataContext is MainPageViewModel viewModel)
        {
            viewModel.UpdateCode();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _renderTimer.Tick -= OnRenderTimerTick;
    }
}
