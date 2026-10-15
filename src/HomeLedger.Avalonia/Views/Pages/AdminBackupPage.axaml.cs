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
        ResultText.Foreground = global::Avalonia.Media.Brush.Parse(success ? "FinanceIncome" : "FinanceExpense");
        ResultText.IsVisible = true;
    }
}
