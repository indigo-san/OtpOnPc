using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;

using FluentAvalonia.Styling;

using System;
using System.Collections.Concurrent;

namespace OtpOnPc.Views;

public enum ImageIconType
{
    Initial,
    //Manual,
    Google,
    Microsoft,
    GitHub,
}

public class ImageIcon : TemplatedControl
{
    private readonly ConcurrentDictionary<string, IBitmap?> _memoryCache = new();

    public static readonly StyledProperty<ImageIconType> IconTypeProperty
        = AvaloniaProperty.Register<ImageIcon, ImageIconType>(nameof(IconType));

    public static readonly StyledProperty<char> InitialCharProperty
        = AvaloniaProperty.Register<ImageIcon, char>(nameof(InitialChar));

    public static readonly StyledProperty<string?> SourceProperty
        = AvaloniaProperty.Register<ImageIcon, string?>(nameof(Source));

    private readonly Pen _pen = new();
    private readonly FluentAvaloniaTheme _theme;

    static ImageIcon()
    {
        AffectsRender<ImageIcon>(IconTypeProperty, InitialCharProperty, SourceProperty);
        MinWidthProperty.OverrideDefaultValue<ImageIcon>(32);
        MinHeightProperty.OverrideDefaultValue<ImageIcon>(32);
    }

    public ImageIcon()
    {
        _theme = AvaloniaLocator.Current.GetRequiredService<FluentAvaloniaTheme>();
    }

    public ImageIconType IconType
    {
        get => GetValue(IconTypeProperty);
        set => SetValue(IconTypeProperty, value);
    }

    public char InitialChar
    {
        get => GetValue(InitialCharProperty);
        set => SetValue(InitialCharProperty, value);
    }

    public string? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var rect = new Rect(Bounds.Size);

        IBitmap? bitmap = null;
        switch (IconType)
        {
            case ImageIconType.Initial:
                if (InitialChar is not '\0')
                {
                    var text = new TextLayout(
                        char.ToUpperInvariant(InitialChar).ToString(),
                        new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                        FontSize,
                        Foreground);

                    var centered = rect.CenterRect(text.Bounds);
                    text.Draw(context, centered.Position + (OperatingSystem.IsWindows() ? new Point(0, -2) : default));
                }

                _pen.Brush = BorderBrush;
                _pen.Thickness = 1.5;

                var min = Math.Min(rect.Width, rect.Height);
                var lineRect = new Rect(0, 0, min, min);
                lineRect = rect.CenterRect(lineRect);
                context.DrawRectangle(null, _pen, lineRect, lineRect.Width / 2, lineRect.Height / 2);
                break;

            //case ImageIconType.Manual:
            //    bitmap = ProvideBitmap(Source);
            //    break;

            case ImageIconType.Google or ImageIconType.Microsoft or ImageIconType.GitHub:
                var url = ToResourceUri(IconType);
                bitmap = ProvideBitmap(url);
                break;

            default:
                break;
        }

        if (bitmap != null)
        {
            Size sourceSize = bitmap.Size;
            Vector scale = Stretch.Uniform.CalculateScaling(rect.Size, sourceSize, StretchDirection.Both);
            Size scaledSize = sourceSize * scale;
            Rect destRect = rect
                .CenterRect(new Rect(scaledSize))
                .Intersect(rect);
            Rect sourceRect = new Rect(sourceSize)
                .CenterRect(new Rect(destRect.Size / scale));

            var interpolationMode = RenderOptions.GetBitmapInterpolationMode(this);

            context.DrawImage(bitmap, sourceRect, destRect, interpolationMode);
        }
    }

    private string? ToResourceUri(ImageIconType type)
    {
        return type switch
        {
            ImageIconType.Google => "avares://OtpOnPc/Resources/Images/Google.png",
            ImageIconType.Microsoft => "avares://OtpOnPc/Resources/Images/Microsoft.png",
            ImageIconType.GitHub => _theme.RequestedTheme == FluentAvaloniaTheme.DarkModeString
                ? "avares://OtpOnPc/Resources/Images/GitHubWhite.png"
                : "avares://OtpOnPc/Resources/Images/GitHub.png",
            _ => null
        };
    }

    private IBitmap? ProvideBitmap(string? url)
    {
        if (url == null)
            return null;

        if (_memoryCache.TryGetValue(url, out var bitmap))
        {
            return bitmap;
        }
        else
        {
            try
            {
                var uri = new Uri(url);
                if (uri.IsAbsoluteUri && uri.IsFile)
                {
                    bitmap = new Bitmap(uri.LocalPath);
                }
                else
                {
                    var assets = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
                    bitmap = new Bitmap(assets.Open(uri));
                }

                _memoryCache[url] = bitmap;

                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
