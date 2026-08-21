using Avalonia.Controls;
using Avalonia.Interactivity;
using HomeLedger.Core.Models;

namespace HomeLedger.Avalonia.Views.Dialogs;

/// <summary>结算确认：列出将被结转的利息合计。</summary>
public partial class SettleDialog : Window
{
    private readonly long _userId;
    private readonly IReadOnlyList<DepositChainView> _chains;

    public SettleDialog(long userId, IReadOnlyList<DepositChainView> chains)
    {
        InitializeComponent();
        _userId = userId;
        _chains = chains;
        DatePicker.SelectedDate = DateTimeOffset.Now;
        DetailText.Text = string.Join("\n", chains.Select(c =>
            $"存入 {c.Principal:0.00} · {c.AnnualRate:0.##}% · 起息 {c.StartDate:yyyy-MM-dd} → 利息 {c.AccruedInterest:0.00}"));
        TotalText.Text = $"合计结转利息：{chains.Sum(c => c.AccruedInterest):0.00}";
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        var date = DateOnly.FromDateTime((DatePicker.SelectedDate ?? DateTimeOffset.Now).Date);
        AppServices.Deposits.Settle(_userId, _chains.Select(c => c.HeadId).ToList(), date, Session.Current!.Id);
        Close(true);
    }
}
