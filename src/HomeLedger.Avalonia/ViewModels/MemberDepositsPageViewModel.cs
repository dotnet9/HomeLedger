using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
using HomeLedger.Avalonia.Commands;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace HomeLedger.Avalonia.ViewModels;

public sealed class DepositRowViewModel
{
    public DepositRowViewModel(DepositChainView chain, Func<DepositChainView, Task> withdraw)
    {
        Chain = chain;
        WithdrawCommand = new AsyncDelegateCommand(() => withdraw(Chain));
    }

    public DepositChainView Chain { get; }

    public string Principal => Chain.Principal.ToString("0.00");

    public string Rate => Chain.AnnualRate.ToString("0.##") + "%";

    public string StartDate => Chain.StartDate.ToString("yyyy-MM-dd");

    public string Accrued => Chain.IsActive ? "+" + Chain.AccruedInterest.ToString("0.00") : "—";

    public string Total => Chain.IsActive ? Chain.Total.ToString("0.00") : Chain.ClosedAt is { } d ? $"已结清（{d:yyyy-MM-dd}）" : "已结清";

    public string ChainText => Chain.ChainText;

    public string Status => Chain.IsActive ? "计息中" : "已结清";

    public IBrush StatusBg => Chain.IsActive ? SolidColorBrush.Parse("#e3f2ec") : SolidColorBrush.Parse("#f0eee8");

    public IBrush StatusFg => Chain.IsActive ? SolidColorBrush.Parse("#2f9e6e") : SolidColorBrush.Parse("#99a096");

    public bool CanOperate => Chain.IsActive;

    public ICommand WithdrawCommand { get; }
}

public sealed class SettlementRowViewModel
{
    public SettlementRowViewModel(Settlement item, string createdByName)
    {
        Item = item;
        By = "操作人：" + createdByName;
    }

    public Settlement Item { get; }

    public string Date => Item.SettledAt.ToString("yyyy-MM-dd");

    public string Count => Item.DepositCount + " 笔";

    public string Total => "+" + Item.TotalInterest.ToString("0.00");

    public string By { get; }
}

public sealed class MemberDepositsPageViewModel : BindableBase
{
    private readonly long _userId;
    private readonly DepositService _deposits;
    private readonly AuthService _auth;
    private string _principalText = "0.00";
    private string _accruedText = "+0.00";
    private string _totalText = "0.00";
    private string _activeCountText = "0";
    private bool _canSettle;
    private List<DepositChainView> _chains = [];

    public MemberDepositsPageViewModel(long userId, DepositService deposits, AuthService auth)
    {
        _userId = userId;
        _deposits = deposits;
        _auth = auth;

        DepositCommand = new AsyncDelegateCommand(DepositAsync);
        WithdrawFirstCommand = new AsyncDelegateCommand(WithdrawFirstAsync, () => DepositRows.Any(r => r.CanOperate));
        SettleCommand = new AsyncDelegateCommand(SettleAsync, () => CanSettle);
        Refresh();
    }

    public Func<Task<bool>>? ShowDepositDialogAsync { get; set; }

    public Func<DepositChainView, Task<bool>>? ShowWithdrawDialogAsync { get; set; }

    public Func<IReadOnlyList<DepositChainView>, Task<bool>>? ShowSettleDialogAsync { get; set; }

    public ObservableCollection<DepositRowViewModel> DepositRows { get; } = [];

    public ObservableCollection<SettlementRowViewModel> SettlementRows { get; } = [];

    public ICommand DepositCommand { get; }

    public AsyncDelegateCommand WithdrawFirstCommand { get; }

    public AsyncDelegateCommand SettleCommand { get; }

    public string PrincipalText
    {
        get => _principalText;
        private set => SetProperty(ref _principalText, value);
    }

    public string AccruedText
    {
        get => _accruedText;
        private set => SetProperty(ref _accruedText, value);
    }

    public string TotalText
    {
        get => _totalText;
        private set => SetProperty(ref _totalText, value);
    }

    public string ActiveCountText
    {
        get => _activeCountText;
        private set => SetProperty(ref _activeCountText, value);
    }

    public bool CanSettle
    {
        get => _canSettle;
        private set => SetProperty(ref _canSettle, value);
    }

    public void Refresh()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _chains = _deposits.ListChains(_userId, today);

        DepositRows.Clear();
        foreach (var chain in _chains)
            DepositRows.Add(new DepositRowViewModel(chain, WithdrawAsync));

        var active = _chains.Where(c => c.IsActive).ToList();
        var principal = active.Sum(c => c.Principal);
        var accrued = active.Sum(c => c.AccruedInterest);
        PrincipalText = principal.ToString("0.00");
        AccruedText = "+" + accrued.ToString("0.00");
        TotalText = (principal + accrued).ToString("0.00");
        ActiveCountText = active.Count.ToString();
        CanSettle = active.Any(c => c.AccruedInterest > 0);
        SettleCommand.RaiseCanExecuteChanged();
        WithdrawFirstCommand.RaiseCanExecuteChanged();

        var users = _auth.ListUsers().ToDictionary(u => u.Id, u => u.DisplayName);
        SettlementRows.Clear();
        foreach (var settlement in _deposits.ListSettlements(_userId))
        {
            var name = users.GetValueOrDefault(settlement.CreatedBy, "—");
            SettlementRows.Add(new SettlementRowViewModel(settlement, name));
        }
    }

    private async Task DepositAsync()
    {
        if (ShowDepositDialogAsync is null) return;
        if (await ShowDepositDialogAsync()) Refresh();
    }

    private async Task WithdrawFirstAsync()
    {
        var active = _chains.FirstOrDefault(c => c.IsActive);
        if (active is null) return;
        await WithdrawAsync(active);
    }

    private async Task WithdrawAsync(DepositChainView chain)
    {
        if (ShowWithdrawDialogAsync is null) return;
        if (await ShowWithdrawDialogAsync(chain)) Refresh();
    }

    private async Task SettleAsync()
    {
        if (ShowSettleDialogAsync is null) return;
        var active = _chains.Where(c => c.IsActive && c.AccruedInterest > 0).ToList();
        if (active.Count == 0) return;
        if (await ShowSettleDialogAsync(active)) Refresh();
    }
}
