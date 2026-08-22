using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
using HomeLedger.Avalonia.Commands;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace HomeLedger.Avalonia.ViewModels;

public sealed class TransactionRowViewModel
{
    public TransactionRowViewModel(TransactionItem item, Func<TransactionItem, Task> edit, Action<TransactionItem> delete)
    {
        Item = item;
        EditCommand = new AsyncDelegateCommand(() => edit(Item));
        DeleteCommand = new DelegateCommand(() => delete(Item));
    }

    public TransactionItem Item { get; }

    public string DateText => Item.Date.ToString("yyyy-MM-dd");

    public string KindText => Item.Kind == TxKind.Income ? "收入" : "支出";

    public IBrush KindBg => Item.Kind == TxKind.Income ? SolidColorBrush.Parse("#e3f2ec") : SolidColorBrush.Parse("#f9e9e4");

    public IBrush KindFg => Item.Kind == TxKind.Income ? SolidColorBrush.Parse("#2f9e6e") : SolidColorBrush.Parse("#c4553d");

    public IBrush AmountBrush => KindFg;

    public string AmountText => (Item.Kind == TxKind.Income ? "+" : "-") + Item.Amount.ToString("0.00");

    public string CategoryName => Item.CategoryName;

    public string Note => Item.Note;

    public ICommand EditCommand { get; }

    public ICommand DeleteCommand { get; }
}

public sealed class MemberLedgerPageViewModel : BindableBase
{
    private readonly long _userId;
    private readonly LedgerService _ledger;
    private DateTimeOffset? _fromDate;
    private DateTimeOffset? _toDate;
    private int _kindFilterIndex;
    private string? _keyword;
    private string _summaryText = "";
    private string _monthIncomeText = "0.00";
    private string _monthExpenseText = "0.00";
    private string _monthBalanceText = "0.00";
    private string _totalBalanceText = "0.00";

    public MemberLedgerPageViewModel(long userId, LedgerService ledger)
    {
        _userId = userId;
        _ledger = ledger;

        AddCommand = new AsyncDelegateCommand(AddAsync);
        FilterCommand = new DelegateCommand(Refresh);

        var now = DateOnly.FromDateTime(DateTime.Today);
        _fromDate = new DateTimeOffset(now.AddDays(-30).ToDateTime(TimeOnly.MinValue));
        _toDate = new DateTimeOffset(now.ToDateTime(TimeOnly.MinValue));
        Refresh();
    }

    public Func<TransactionItem?, Task<bool>>? ShowTransactionDialogAsync { get; set; }

    public ObservableCollection<TransactionRowViewModel> Transactions { get; } = [];

    public ICommand AddCommand { get; }

    public DelegateCommand FilterCommand { get; }

    public DateTimeOffset? FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTimeOffset? ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public int KindFilterIndex
    {
        get => _kindFilterIndex;
        set => SetProperty(ref _kindFilterIndex, value);
    }

    public string? Keyword
    {
        get => _keyword;
        set => SetProperty(ref _keyword, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public string MonthIncomeText
    {
        get => _monthIncomeText;
        private set => SetProperty(ref _monthIncomeText, value);
    }

    public string MonthExpenseText
    {
        get => _monthExpenseText;
        private set => SetProperty(ref _monthExpenseText, value);
    }

    public string MonthBalanceText
    {
        get => _monthBalanceText;
        private set => SetProperty(ref _monthBalanceText, value);
    }

    public string TotalBalanceText
    {
        get => _totalBalanceText;
        private set => SetProperty(ref _totalBalanceText, value);
    }

    public void Refresh()
    {
        var from = FromDate is { } f ? DateOnly.FromDateTime(f.Date) : (DateOnly?)null;
        var to = ToDate is { } t ? DateOnly.FromDateTime(t.Date) : (DateOnly?)null;
        var kind = KindFilterIndex switch { 1 => TxKind.Income, 2 => TxKind.Expense, _ => (TxKind?)null };
        var items = _ledger.ListTransactions(new TransactionFilter
        {
            UserId = _userId,
            From = from,
            To = to,
            Kind = kind,
            Keyword = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim(),
        });

        Transactions.Clear();
        foreach (var item in items)
            Transactions.Add(new TransactionRowViewModel(item, EditAsync, Delete));

        if (from is { } f2 && to is { } t2)
        {
            var summary = _ledger.Summary(_userId, f2, t2);
            SummaryText = $"区间收入 {summary.Income:0.00} · 支出 {summary.Expense:0.00} · 结余 {summary.Balance:0.00}，共 {items.Count} 条";
        }
        else
        {
            SummaryText = $"共 {items.Count} 条（选择日期区间可查看汇总）";
        }

        var monthStart = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var month = _ledger.Summary(_userId, monthStart, monthStart.AddMonths(1).AddDays(-1));
        MonthIncomeText = month.Income.ToString("0.00");
        MonthExpenseText = month.Expense.ToString("0.00");
        MonthBalanceText = month.Balance.ToString("0.00");

        var all = _ledger.Summary(_userId, new DateOnly(1, 1, 1), new DateOnly(9999, 12, 31));
        TotalBalanceText = all.Balance.ToString("0.00");
    }

    private async Task AddAsync()
    {
        if (ShowTransactionDialogAsync is null) return;
        if (await ShowTransactionDialogAsync(null)) Refresh();
    }

    private async Task EditAsync(TransactionItem item)
    {
        if (ShowTransactionDialogAsync is null) return;
        if (await ShowTransactionDialogAsync(item)) Refresh();
    }

    private void Delete(TransactionItem item)
    {
        _ledger.DeleteTransaction(_userId, item.Id);
        Refresh();
    }
}
