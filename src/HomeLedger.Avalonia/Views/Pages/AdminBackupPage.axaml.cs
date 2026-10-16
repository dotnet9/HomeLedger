using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HomeLedger.Avalonia.Views;
using HomeLedger.Core.Services;

namespace HomeLedger.Avalonia.Views.Pages;

public partial class AdminBackupPage : UserControl
{
    private string _resultResourceKey = "FinanceIncome";

    public AdminBackupPage()
    {
        InitializeComponent();
        DbPathText.Text = AppServices.DbPath;
        ResultText.ActualThemeVariantChanged += (_, _) => UpdateResultForeground();
        Dispatcher.UIThread.Post(() => BackupButton.Focus());
    }

    private async void OnBackup(object? sender, RoutedEventArgs e)
    {
        var storage = TopLevelHost.TryGetStorageProvider(this);
        if (storage is null)
        {
            ShowResult(AppText.Get(Language.FolderPickerUnavailable), false);
            return;
        }

        IReadOnlyList<IStorageFolder> folder;
        try
        {
            folder = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = AppText.Get(Language.ChooseBackupFolder) });
        }
        catch (Exception ex)
        {
            ShowResult(AppText.Get(Language.FolderPickerFailed) + ex.Message, false);
            return;
        }
        if (folder.Count == 0) return;
        var target = folder[0].TryGetLocalPath();
        if (target is null)
        {
            ShowResult(AppText.Get(Language.LocalBackupRequired), false);
            return;
        }
        try
        {
            var file = BackupService.Backup(AppServices.DbPath, target);
            ShowResult(AppText.Get(Language.BackupSucceeded) + file, true);
        }
        catch (Exception ex)
        {
            ShowResult(AppText.Get(Language.BackupFailed) + ex.Message, false);
        }
    }

    private void ShowResult(string message, bool success)
    {
        ResultText.Text = message;
        _resultResourceKey = success ? "FinanceIncome" : "FinanceExpense";
        UpdateResultForeground();
        ResultText.IsVisible = true;
    }

    private void UpdateResultForeground()
    {
        // 资源键不是颜色字符串；切换主题时重新读取，缺失时继承页面文字颜色。
        ResultText.Foreground = ResultText.TryFindResource(_resultResourceKey, ResultText.ActualThemeVariant, out var value)
            && value is IBrush brush ? brush : Foreground;
    }
}
