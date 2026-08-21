using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using HomeLedger.Avalonia.Views.Dialogs;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;

namespace HomeLedger.Avalonia.Views.Pages;

public class DepositRow
{
    public required DepositChainView Chain { get; init; }
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
    public required IRelayCommand WithdrawCommand { get; init; }
}

public class SettlementRow
{
    public required Settlement Item { get; init; }
    public string Date => Item.SettledAt.ToString("yyyy-MM-dd");
    public string Count => Item.DepositCount + " 笔";
    public string Total => "+" + Item.TotalInterest.ToString("0.00");
    public string By => "操作人：" + (AppServices.Auth.ListUsers().FirstOrDefault(u => u.Id == Item.CreatedBy)?.DisplayName ?? "—");
}

public partial class MemberDepositsPage : UserControl
{
    private readonly long _userId;

    public MemberDepositsPage()
    {
        InitializeComponent();
        _userId = Session.Current!.Id;
        Refresh();
    }

    private void Refresh()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var chains = AppServices.Deposits.ListChains(_userId, today);
        ListHost.ItemsSource = chains.Select(c => new DepositRow
        {
            Chain = c,
            WithdrawCommand = new RelayCommand(() => Withdraw(c)),
        }).ToList();

        var active = chains.Where(c => c.IsActive).ToList();
        var principal = active.Sum(c => c.Principal);
        var accrued = active.Sum(c => c.AccruedInterest);
        PrincipalText.Text = principal.ToString("0.00");
        AccruedText.Text = "+" + accrued.ToString("0.00");
        TotalText.Text = (principal + accrued).ToString("0.00");
        ActiveCountText.Text = active.Count.ToString();
        SettleButton.IsEnabled = active.Any(c => c.AccruedInterest > 0);

        SettleListHost.ItemsSource = AppServices.Deposits.ListSettlements(_userId).Select(s => new SettlementRow { Item = s }).ToList();
    }

    private async void OnDeposit(object? sender, RoutedEventArgs e)
    {
        var dialog = new DepositDialog(_userId) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        if (await dialog.ShowDialog<bool>(Owner())) Refresh();
    }

    private async void Withdraw(DepositChainView chain)
    {
        var dialog = new WithdrawDialog(_userId, chain) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        if (await dialog.ShowDialog<bool>(Owner())) Refresh();
    }

    private async void OnWithdraw(object? sender, RoutedEventArgs e)
    {
        var active = AppServices.Deposits.ListChains(_userId, DateOnly.FromDateTime(DateTime.Today)).FirstOrDefault(c => c.IsActive);
        if (active is null) return;
        Withdraw(active);
    }

    private async void OnSettle(object? sender, RoutedEventArgs e)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var active = AppServices.Deposits.ListChains(_userId, today).Where(c => c.IsActive && c.AccruedInterest > 0).ToList();
        if (active.Count == 0) return;
        var dialog = new SettleDialog(_userId, active) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        if (await dialog.ShowDialog<bool>(Owner())) Refresh();
    }

    private Window Owner() => VisualRoot as Window ?? throw new InvalidOperationException();
}
