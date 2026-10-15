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
                viewModel.ShowStatus(AppText.Get(Language.SavePickerUnavailable), true);
            return null;
        }

        IStorageFile? file;
        try
        {
            file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = AppText.Format(Language.DetailsFileName, DateTime.Today),
                FileTypeChoices = [new FilePickerFileType("CSV") { Patterns = ["*.csv"] }],
            });
        }
        catch (Exception ex)
        {
            if (DataContext is AdminDetailsPageViewModel viewModel3)
                viewModel3.ShowStatus(AppText.Get(Language.SavePickerFailed) + ex.Message, true);
            return null;
        }
        if (file is null) return null;

        var path = file.TryGetLocalPath();
        if (path is null && DataContext is AdminDetailsPageViewModel viewModel2)
            viewModel2.ShowStatus(AppText.Get(Language.LocalExportRequired), true);
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
