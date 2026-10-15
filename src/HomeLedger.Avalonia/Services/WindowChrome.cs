using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using CodeWF.Avalonia.Controls.Controls;
using CodeWF.Avalonia.Lang.MarkupExtensions;
using HomeLedger.Avalonia.Views;

namespace HomeLedger.Avalonia.Services;

/// <summary>标题栏使用动态语言绑定，登录页也可访问外观和关于。</summary>
public static class WindowChrome
{
    public static void Apply(CodeWFWindow window, string key, bool actions = false)
    {
        var title = new TextBlock { FontSize = 14, Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        title.Bind(TextBlock.TextProperty, new I18nExtension(key).CreateBinding());
        window.LeftContent = title;
        if (!actions) return;
        var appearance = new Button { Margin = new Thickness(0, 0, 8, 0), FontSize = 12 };
        appearance.Bind(ContentControl.ContentProperty, new I18nExtension(Language.AppearanceTitle).CreateBinding());
        appearance.Click += (_, _) => new AppearanceWindow().Show(window);
        var about = new Button { Margin = new Thickness(0, 0, 8, 0), FontSize = 12 };
        about.Bind(ContentControl.ContentProperty, new I18nExtension(Language.AboutTitle).CreateBinding());
        about.Click += (_, _) => new AboutWindow().Show(window);
        window.RightContent = new StackPanel { Orientation = Orientation.Horizontal, Children = { appearance, about } };
    }
}
