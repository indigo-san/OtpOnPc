using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.VisualTree;

using FluentAvalonia.UI.Controls;
using System;
using System.Threading.Tasks;

namespace OtpOnPc;

public sealed class ExceptionDialog : TaskDialog, IStyleable
{
    public ExceptionDialog(IVisual? xamlRoot, Exception exception)
    {
        XamlRoot = xamlRoot;
        Header = "例外が発生しました";
        Content = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = """
                           ハンドルされない例外が発生しました。
                           この例外を無視して、アプリケーションを継続しますか？
                           """
                },
                new Expander
                {
                    Header = "詳細を表示",
                    Content = new ScrollViewer
                    {
                        Content = new SelectableTextBlock
                        {
                            Text = exception.ToString()
                        }
                    }
                }
            }
        };

        Buttons.Add(new TaskDialogButton("はい", ExceptionDialogResult.Continue));
        Buttons.Add(new TaskDialogButton("終了", ExceptionDialogResult.Shutdown));
    }

    Type IStyleable.StyleKey => typeof(TaskDialog);

    public static async Task<ExceptionDialogResult> Handle(Exception exception, IVisual? xamlRoot = null)
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        xamlRoot ??= lifetime?.MainWindow;

        return (ExceptionDialogResult)await new ExceptionDialog(xamlRoot, exception).ShowAsync();
    }
}
