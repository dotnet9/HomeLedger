using Avalonia.Controls;
using Avalonia.Threading;
using HomeLedger.Avalonia.Views;
using HomeLedger.Avalonia.ViewModels;
using HomeLedger.Avalonia.Views.Dialogs;
using HomeLedger.Core.Models;

namespace HomeLedger.Avalonia.Views.Pages;

public partial class MemberDepositsPage : UserControl
{
    private readonly long _userId;

    public MemberDepositsPage()
    {
        InitializeComponent();
        _userId = Session.Current!.Id;
        var viewModel = new MemberDepositsPageViewModel(_userId, AppServices.Deposits, AppServices.Auth)
        {
            ShowDepositDialogAsync = ShowDepositDialogAsync,
            ShowWithdrawDialogAsync = ShowWithdrawDialogAsync,
            ShowSettleDialogAsync = ShowSettleDialogAsync,
        };
        DataContext = viewModel;
        Dispatcher.UIThread.Post(() => DepositButton.Focus());
    }

    private async Task<bool> ShowDepositDialogAsync()
    {
        var dialog = new DepositDialog(_userId) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        return await dialog.ShowDialog<bool>(Owner());
    }

    private async Task<bool> ShowWithdrawDialogAsync(DepositChainView chain)
    {
        var dialog = new WithdrawDialog(_userId, chain) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        return await dialog.ShowDialog<bool>(Owner());
    }

    private async Task<bool> ShowSettleDialogAsync(IReadOnlyList<DepositChainView> chains)
    {
        var dialog = new SettleDialog(_userId, chains) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        return await dialog.ShowDialog<bool>(Owner());
    }

    private Window Owner() => TopLevelHost.GetOwnerWindow(this);
}
