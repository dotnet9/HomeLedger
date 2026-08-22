using Avalonia.Controls;
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
        DataContext = viewModel;
    }

    private async Task<bool> ShowTransactionDialogAsync(TransactionItem? item)
    {
        var dialog = new TransactionDialog(_userId, item) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        return await dialog.ShowDialog<bool>(VisualRoot as Window ?? throw new InvalidOperationException());
    }
}
