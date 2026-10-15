using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace HomeLedger.Avalonia.Views;

internal static class TopLevelHost
{
    public static Window GetOwnerWindow(Control control)
    {
        return TopLevel.GetTopLevel(control) as Window
               ?? throw new InvalidOperationException(AppText.Get(Language.DialogHostUnavailable));
    }

    public static IStorageProvider? TryGetStorageProvider(Control control)
    {
        return TopLevel.GetTopLevel(control)?.StorageProvider;
    }
}
