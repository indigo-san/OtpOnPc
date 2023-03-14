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
                Header = "暗号化方法を強制的に変更しますか？",
                Content =
                    """
                    暗号化方法を強制的に変更すると、保存されているアカウントが削除されます。
                    通常、この操作は何らかの理由で暗号化方法が変わってしまった場合に使います。
                    パスワードを忘れてしまった場合は諦めてください。
                    """,
                Buttons =
                {
                    new TaskDialogButton("はい", true),
                    new TaskDialogButton("いいえ", false)
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
