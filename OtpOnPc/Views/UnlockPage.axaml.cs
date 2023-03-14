using Avalonia.Controls;
using Avalonia.Interactivity;

using FluentAvalonia.UI.Controls;

using OtpOnPc.ViewModels;

using System;
using System.Threading.Tasks;

namespace OtpOnPc.Views;

public partial class UnlockPage : UserControl
{
    public UnlockPage()
    {
        InitializeComponent();
    }

    private async void ResetClick(object? sender, RoutedEventArgs e)
    {
        await new ResetDialog(VisualRoot).ShowAsync();
    }

    private async Task SwitchMethod(Func<UnlockPageViewModel, Task> action)
    {
        if (DataContext is UnlockPageViewModel viewModel)
        {
            var dialog = new TaskDialog
            {
                XamlRoot = VisualRoot,
                Header = "�Í������@�������I�ɕύX���܂����H",
                Content =
                    """
                    �Í������@�������I�ɕύX����ƁA�ۑ�����Ă���A�J�E���g���폜����܂��B
                    �ʏ�A���̑���͉��炩�̗��R�ňÍ������@���ς���Ă��܂����ꍇ�Ɏg���܂��B
                    �p�X���[�h��Y��Ă��܂����ꍇ�͒��߂Ă��������B
                    """,
                Buttons =
                {
                    new TaskDialogButton("�͂�", true),
                    new TaskDialogButton("������", false)
                }
            };

            if ((await dialog.ShowAsync()) is true)
            {
                await action(viewModel);
            }
        }

        useAesRadio.IsChecked = true;
    }

    private async void UseNoPassAesClick(object? sender, RoutedEventArgs e)
    {
        await SwitchMethod(v => v.UseNoPasswordAes());
    }

    private async void UseWSCDClick(object? sender, RoutedEventArgs e)
    {
        await SwitchMethod(v => v.UseWSCD());
    }
}
