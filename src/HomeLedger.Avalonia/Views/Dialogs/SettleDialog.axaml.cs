using Avalonia;
using Avalonia.Controls;
using CodeWF.Avalonia.Controls.Controls;
using Avalonia.Interactivity;
using HomeLedger.Core.Models;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;

namespace HomeLedger.Avalonia.Views.Dialogs;

/// <summary>结算确认：列出将被结转的利息合计。</summary>
public partial class SettleDialog : CodeWFWindow
{
    private readonly long _userId;
    private readonly IReadOnlyList<DepositChainView> _chains;

    public SettleDialog()
        : this(Session.Current?.Id ?? 0, [])
    {
    }

    public SettleDialog(long userId, IReadOnlyList<DepositChainView> chains)
    {
        InitializeComponent();
        HomeLedger.Avalonia.Services.WindowChrome.Apply(this, Language.SettleInterest);
        _userId = userId;
        _chains = chains;
        DatePicker.SelectedDate = DateTimeOffset.Now;
        DetailText.Text = string.Join("\n", chains.Select(c =>
            AppText.Format(Language.SettlementItem, c.Principal, c.AnnualRate, c.StartDate, c.AccruedInterest)));
        TotalText.Text = AppText.Format(Language.SettlementTotal, chains.Sum(c => c.AccruedInterest));
        Dispatcher.UIThread.Post(() => ConfirmButton.Focus());
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        var date = DateOnly.FromDateTime((DatePicker.SelectedDate ?? DateTimeOffset.Now).Date);
        AppServices.Deposits.Settle(_userId, _chains.Select(c => c.HeadId).ToList(), date, Session.Current!.Id);
        Close(true);
    }
}
