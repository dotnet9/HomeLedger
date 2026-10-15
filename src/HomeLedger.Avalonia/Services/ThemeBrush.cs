using Avalonia;
using Avalonia.Media;

namespace HomeLedger.Avalonia.Services;

/// <summary>自绘图表和视图模型从共享主题读取语义画刷。</summary>
public static class ThemeBrush
{
    public static IBrush Get(string key) => Application.Current is { } app &&
        app.TryGetResource(key, app.ActualThemeVariant, out var value) && value is IBrush brush ? brush : Brushes.Gray;
}
