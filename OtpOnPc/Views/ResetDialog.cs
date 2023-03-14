using Avalonia;
using Avalonia.Styling;
using Avalonia.VisualTree;

using FluentAvalonia.UI.Controls;

using OtpOnPc.Services;

using System;

namespace OtpOnPc.Views;

public sealed class ResetDialog : TaskDialog, IStyleable
{
    public ResetDialog(IVisual? xamlRoot)
    {
        XamlRoot = xamlRoot;
        Header = "リセットしますか？";
        Content = """
            追加したアカウント、設定をすべて初期化します。
            この操作を実行しても、アカウントの二段階認証は無効になりません。
            操作を実行する前にアカウントの二段階認証が
            無効化されていることを確認してください。
            """;
        var yes = new TaskDialogButton("はい", true);
        yes.Click += YesClick;

        Buttons.Add(yes);
        Buttons.Add(new TaskDialogButton("いいえ", false));
    }

    Type IStyleable.StyleKey => typeof(TaskDialog);

    private async void YesClick(TaskDialogButton sender, System.EventArgs args)
    {
        var manager = AvaloniaLocator.Current.GetRequiredService<TotpModelManager>();
        await manager.Reset();

        var settings = AvaloniaLocator.Current.GetRequiredService<SettingsService>();
        await settings.Reset();
    }
}
