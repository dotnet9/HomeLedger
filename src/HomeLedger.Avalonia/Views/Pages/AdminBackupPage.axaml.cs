using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HomeLedger.Avalonia.Views;
using HomeLedger.Core.Services;

namespace HomeLedger.Avalonia.Views.Pages;

public partial class AdminBackupPage : UserControl
{
    public AdminBackupPage()
    {
        InitializeComponent();
        DbPathText.Text = AppServices.DbPath;
        Dispatcher.UIThread.Post(() => BackupButton.Focus());
    }

    private async void OnBackup(object? sender, RoutedEventArgs e)
    {
        var storage = TopLevelHost.TryGetStorageProvider(this);
        if (storage is null)
        {
            ShowResult("无法打开目录选择窗口，请稍后重试", false);
            return;
        }

        IReadOnlyList<IStorageFolder> folder;
        try
        {
            folder = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "选择备份目录" });
        }
        catch (Exception ex)
        {
            ShowResult("打开目录选择窗口失败：" + ex.Message, false);
            return;
        }
        if (folder.Count == 0) return;
        var target = folder[0].TryGetLocalPath();
        if (target is null)
        {
            ShowResult("请选择本机磁盘目录后再备份", false);
            return;
        }
        try
        {
            var file = BackupService.Backup(AppServices.DbPath, target);
            ShowResult("备份成功：" + file, true);
        }
        catch (Exception ex)
        {
            ShowResult("备份失败：" + ex.Message, false);
        }
    }

    private void ShowResult(string message, bool success)
    {
        ResultText.Text = message;
        ResultText.Foreground = global::Avalonia.Media.Brush.Parse(success ? "#2f9e6e" : "#c4553d");
        ResultText.IsVisible = true;
    }
}
