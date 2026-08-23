using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace HomeLedger.Avalonia.Views;

internal static class TopLevelHost
{
    public static Window GetOwnerWindow(Control control)
    {
        return TopLevel.GetTopLevel(control) as Window
               ?? throw new InvalidOperationException("当前页面尚未附加到主窗口，无法打开对话框。");
    }

    public static IStorageProvider? TryGetStorageProvider(Control control)
    {
        return TopLevel.GetTopLevel(control)?.StorageProvider;
    }
}
