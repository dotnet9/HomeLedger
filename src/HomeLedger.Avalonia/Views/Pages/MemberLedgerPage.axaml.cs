using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using HomeLedger.Avalonia.Views;
using HomeLedger.Avalonia.ViewModels;
using HomeLedger.Avalonia.Views.Dialogs;
using HomeLedger.Core.Models;

namespace HomeLedger.Avalonia.Views.Pages;

public partial class MemberLedgerPage : UserControl
{
    private readonly long _userId;

    public MemberLedgerPage()
    {
        InitializeComponent();
        _userId = Session.Current!.Id;
        var viewModel = new MemberLedgerPageViewModel(_userId, AppServices.Ledger)
        {
            ShowTransactionDialogAsync = ShowTransactionDialogAsync
        };
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MemberLedgerPageViewModel.QuickMessage))
                Dispatcher.UIThread.Post(FocusQuickAmount);
        };
        DataContext = viewModel;
        Dispatcher.UIThread.Post(FocusQuickAmount);
    }

    private async Task<bool> ShowTransactionDialogAsync(TransactionItem? item)
    {
        var dialog = new TransactionDialog(_userId, item) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        return await dialog.ShowDialog<bool>(TopLevelHost.GetOwnerWindow(this));
    }

    private void OnQuickFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not MemberLedgerPageViewModel viewModel) return;
        if (!viewModel.SaveQuickCommand.CanExecute(null)) return;

        viewModel.SaveQuickCommand.Execute(null);
        e.Handled = true;
    }

    private void OnFilterKeywordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not MemberLedgerPageViewModel viewModel) return;

        ExecuteFilter(viewModel);
        e.Handled = true;
    }

    private void OnFilterAreaKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.Source is Button || DataContext is not MemberLedgerPageViewModel viewModel) return;

        ExecuteFilter(viewModel);
        e.Handled = true;
    }

    private static void ExecuteFilter(MemberLedgerPageViewModel viewModel)
    {
        if (viewModel.FilterCommand.CanExecute())
            viewModel.FilterCommand.Execute();
    }

    private void FocusQuickAmount()
    {
        QuickAmountBox.Focus();
        QuickAmountBox.SelectAll();
    }
}
