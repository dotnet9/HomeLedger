using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using HomeLedger.Avalonia.Views.Dialogs;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;

namespace HomeLedger.Avalonia.Views.Pages;

public class TransactionRow
{
    public required TransactionItem Item { get; init; }
    public string DateText => Item.Date.ToString("yyyy-MM-dd");
    public string KindText => Item.Kind == TxKind.Income ? "收入" : "支出";
    public IBrush KindBg => Item.Kind == TxKind.Income ? SolidColorBrush.Parse("#e3f2ec") : SolidColorBrush.Parse("#f9e9e4");
    public IBrush KindFg => Item.Kind == TxKind.Income ? SolidColorBrush.Parse("#2f9e6e") : SolidColorBrush.Parse("#c4553d");
    public IBrush AmountBrush => KindFg;
    public string AmountText => (Item.Kind == TxKind.Income ? "+" : "-") + Item.Amount.ToString("0.00");
    public string CategoryName => Item.CategoryName;
    public string Note => Item.Note;
    public required IRelayCommand EditCommand { get; init; }
    public required IRelayCommand DeleteCommand { get; init; }
}

public partial class MemberLedgerPage : UserControl
{
    private readonly long _userId;
    private DateOnly? _from, _to;

    public MemberLedgerPage()
    {
        InitializeComponent();
        _userId = Session.Current!.Id;
        var now = DateOnly.FromDateTime(DateTime.Today);
        FromDate.SelectedDate = new DateTimeOffset(now.AddDays(-30).ToDateTime(TimeOnly.MinValue));
        ToDate.SelectedDate = new DateTimeOffset(now.ToDateTime(TimeOnly.MinValue));
        Refresh();
    }

    private void Refresh()
    {
        _from = FromDate.SelectedDate is { } f ? DateOnly.FromDateTime(f.Date) : null;
        _to = ToDate.SelectedDate is { } t ? DateOnly.FromDateTime(t.Date) : null;
        var kind = (KindBox.SelectedIndex) switch { 1 => TxKind.Income, 2 => TxKind.Expense, _ => (TxKind?)null };
        var items = AppServices.Ledger.ListTransactions(new TransactionFilter
        {
            UserId = _userId,
            From = _from,
            To = _to,
            Kind = kind,
            Keyword = string.IsNullOrWhiteSpace(KeywordBox.Text) ? null : KeywordBox.Text.Trim(),
        });

        ListHost.ItemsSource = items.Select(t => new TransactionRow
        {
            Item = t,
            EditCommand = new RelayCommand(() => Edit(t)),
            DeleteCommand = new RelayCommand(() => Delete(t)),
        }).ToList();

        if (_from is { } f2 && _to is { } t2)
        {
            var s = AppServices.Ledger.Summary(_userId, f2, t2);
            SummaryText.Text = $"区间收入 {s.Income:0.00} · 支出 {s.Expense:0.00} · 结余 {s.Balance:0.00}，共 {items.Count} 条";
        }
        else SummaryText.Text = $"共 {items.Count} 条（选择日期区间可查看汇总）";

        var monthStart = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var month = AppServices.Ledger.Summary(_userId, monthStart, monthStart.AddMonths(1).AddDays(-1));
        MonthIncomeText.Text = month.Income.ToString("0.00");
        MonthExpenseText.Text = month.Expense.ToString("0.00");
        MonthBalanceText.Text = month.Balance.ToString("0.00");
        var all = AppServices.Ledger.Summary(_userId, new DateOnly(1, 1, 1), new DateOnly(9999, 12, 31));
        TotalBalanceText.Text = all.Balance.ToString("0.00");
    }

    private async void OnAdd(object? sender, RoutedEventArgs e)
    {
        var dialog = new TransactionDialog(_userId) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        if (await dialog.ShowDialog<bool>(VisualRoot as Window ?? throw new InvalidOperationException())) Refresh();
    }

    private async void Edit(TransactionItem item)
    {
        var dialog = new TransactionDialog(_userId, item) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        if (await dialog.ShowDialog<bool>(VisualRoot as Window ?? throw new InvalidOperationException())) Refresh();
    }

    private void Delete(TransactionItem item)
    {
        AppServices.Ledger.DeleteTransaction(_userId, item.Id);
        Refresh();
    }

    private void OnFilter(object? sender, RoutedEventArgs e) => Refresh();
}
