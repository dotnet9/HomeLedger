using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HomeLedger.Avalonia.Views;
using HomeLedger.Avalonia.ViewModels;

namespace HomeLedger.Avalonia.Views.Pages;

public partial class AdminDetailsPage : UserControl
{
    public AdminDetailsPage()
    {
        InitializeComponent();
        DataContext = new AdminDetailsPageViewModel(AppServices.Ledger, AppServices.Auth)
        {
            PickCsvPathAsync = PickCsvPathAsync,
        };
        Dispatcher.UIThread.Post(() => KeywordBox.Focus());
    }

    private async Task<string?> PickCsvPathAsync()
    {
        var storage = TopLevelHost.TryGetStorageProvider(this);
        if (storage is null)
        {
            if (DataContext is AdminDetailsPageViewModel viewModel)
                viewModel.ShowStatus("无法打开文件保存窗口，请稍后重试", true);
            return null;
        }

        IStorageFile? file;
        try
        {
            file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = $"HomeLedger-明细-{DateTime.Today:yyyyMMdd}",
                FileTypeChoices = [new FilePickerFileType("CSV") { Patterns = ["*.csv"] }],
            });
        }
        catch (Exception ex)
        {
            if (DataContext is AdminDetailsPageViewModel viewModel3)
                viewModel3.ShowStatus("打开保存窗口失败：" + ex.Message, true);
            return null;
        }
        if (file is null) return null;

        var path = file.TryGetLocalPath();
        if (path is null && DataContext is AdminDetailsPageViewModel viewModel2)
            viewModel2.ShowStatus("请选择本机磁盘路径后再导出", true);
        return path;
    }

    private void OnKeywordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not AdminDetailsPageViewModel viewModel) return;

        ExecuteFilter(viewModel);
        e.Handled = true;
    }

    private void OnFilterAreaKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.Source is Button || DataContext is not AdminDetailsPageViewModel viewModel) return;

        ExecuteFilter(viewModel);
        e.Handled = true;
    }

    private static void ExecuteFilter(AdminDetailsPageViewModel viewModel)
    {
        if (viewModel.FilterCommand.CanExecute(null))
            viewModel.FilterCommand.Execute(null);
    }
}
