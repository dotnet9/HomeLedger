using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using HomeLedger.Core.Services;

namespace HomeLedger.Avalonia.Views.Pages;

public partial class AdminBackupPage : UserControl
{
    public AdminBackupPage()
    {
        InitializeComponent();
        DbPathText.Text = AppServices.DbPath;
    }

    private async void OnBackup(object? sender, RoutedEventArgs e)
    {
        var storage = (VisualRoot as Window)!.StorageProvider;
        var folder = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "选择备份目录" });
        if (folder.Count == 0) return;
        var target = folder[0].TryGetLocalPath();
        if (target is null) return;
        try
        {
            var file = BackupService.Backup(AppServices.DbPath, target);
            ResultText.Text = "备份成功：" + file;
            ResultText.Foreground = global::Avalonia.Media.Brush.Parse("#2f9e6e");
        }
        catch (Exception ex)
        {
            ResultText.Text = "备份失败：" + ex.Message;
            ResultText.Foreground = global::Avalonia.Media.Brush.Parse("#c4553d");
        }
        ResultText.IsVisible = true;
    }
}
