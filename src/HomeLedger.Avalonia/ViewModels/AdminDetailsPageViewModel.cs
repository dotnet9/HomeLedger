using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using Avalonia.Media;
using HomeLedger.Avalonia.Commands;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace HomeLedger.Avalonia.ViewModels;

public sealed class UserOptionViewModel
{
    public UserOptionViewModel(long? id, string name)
    {
        Id = id;
        Name = name;
    }

    public long? Id { get; }

    public string Name { get; }
}

public sealed class AdminTransactionRowViewModel
{
    public AdminTransactionRowViewModel(TransactionItem item, string memberName)
    {
        Item = item;
        MemberName = memberName;
    }

    public TransactionItem Item { get; }

    public string MemberName { get; }

    public string DateText => Item.Date.ToString("yyyy-MM-dd");

    public string KindText => Item.Kind == TxKind.Income ? "收入" : "支出";

    public IBrush KindBg => Item.Kind == TxKind.Income ? SolidColorBrush.Parse("#e3f2ec") : SolidColorBrush.Parse("#f9e9e4");

    public IBrush KindFg => Item.Kind == TxKind.Income ? SolidColorBrush.Parse("#2f9e6e") : SolidColorBrush.Parse("#c4553d");

    public string CategoryName => string.IsNullOrWhiteSpace(Item.CategoryName) ? "未分类" : Item.CategoryName;

    public string AmountText => (Item.Kind == TxKind.Income ? "+" : "-") + Item.Amount.ToString("0.00");

    public IBrush AmountBrush => KindFg;

    public string Note => Item.Note;
}

public sealed class AdminDetailsPageViewModel : BindableBase
{
    private readonly LedgerService _ledger;
    private readonly AuthService _auth;
    private readonly Dictionary<long, string> _memberNames;
    private DateTimeOffset? _fromDate;
    private DateTimeOffset? _toDate;
    private int _memberFilterIndex;
    private int _kindFilterIndex;
    private int _categoryFilterIndex;
    private string? _keyword;
    private string _summaryText = "";
    private string? _statusMessage;
    private bool _isStatusError;

    public AdminDetailsPageViewModel(LedgerService ledger, AuthService auth)
    {
        _ledger = ledger;
        _auth = auth;
        FilterCommand = new DelegateCommand(Refresh);
        ExportCsvCommand = new AsyncDelegateCommand(ExportCsvAsync, () => Transactions.Count > 0);

        _memberNames = _auth.ListUsers()
            .Where(u => u.Role == Role.Member)
            .ToDictionary(u => u.Id, u => u.DisplayName);

        var today = DateOnly.FromDateTime(DateTime.Today);
        _fromDate = new DateTimeOffset(new DateOnly(today.Year, today.Month, 1).ToDateTime(TimeOnly.MinValue));
        _toDate = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue));
        LoadMembers();
        LoadCategories();
        Refresh();
    }

    public Func<Task<string?>>? PickCsvPathAsync { get; set; }

    public ObservableCollection<UserOptionViewModel> Members { get; } = [];

    public ObservableCollection<CategoryOptionViewModel> Categories { get; } = [];

    public ObservableCollection<AdminTransactionRowViewModel> Transactions { get; } = [];

    public ICommand FilterCommand { get; }

    public AsyncDelegateCommand ExportCsvCommand { get; }

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

    public int MemberFilterIndex
    {
        get => _memberFilterIndex;
        set => SetProperty(ref _memberFilterIndex, value);
    }

    public int KindFilterIndex
    {
        get => _kindFilterIndex;
        set
        {
            if (!SetProperty(ref _kindFilterIndex, value)) return;
            LoadCategories();
            Refresh();
        }
    }

    public int CategoryFilterIndex
    {
        get => _categoryFilterIndex;
        set => SetProperty(ref _categoryFilterIndex, value);
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

    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (!SetProperty(ref _statusMessage, value)) return;
            RaisePropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public IBrush StatusBrush => _isStatusError ? SolidColorBrush.Parse("#c4553d") : SolidColorBrush.Parse("#2f9e6e");

    public void Refresh()
    {
        var from = FromDate is { } f ? DateOnly.FromDateTime(f.Date) : (DateOnly?)null;
        var to = ToDate is { } t ? DateOnly.FromDateTime(t.Date) : (DateOnly?)null;
        var memberId = MemberFilterIndex >= 0 && MemberFilterIndex < Members.Count ? Members[MemberFilterIndex].Id : null;
        var kind = KindFilterIndex switch { 1 => TxKind.Income, 2 => TxKind.Expense, _ => (TxKind?)null };
        var categoryId = CategoryFilterIndex >= 0 && CategoryFilterIndex < Categories.Count ? Categories[CategoryFilterIndex].Id : null;

        var items = _ledger.ListTransactions(new TransactionFilter
        {
            UserId = memberId,
            From = from,
            To = to,
            Kind = kind,
            CategoryId = categoryId,
            Keyword = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim(),
            Limit = 2000,
        });

        Transactions.Clear();
        foreach (var item in items)
        {
            var memberName = _memberNames.GetValueOrDefault(item.UserId, $"成员 {item.UserId}");
            Transactions.Add(new AdminTransactionRowViewModel(item, memberName));
        }

        var income = items.Where(i => i.Kind == TxKind.Income).Sum(i => i.Amount);
        var expense = items.Where(i => i.Kind == TxKind.Expense).Sum(i => i.Amount);
        SummaryText = $"共 {items.Count} 条 · 收入 {income:0.00} · 支出 {expense:0.00} · 结余 {income - expense:0.00}";
        ExportCsvCommand.RaiseCanExecuteChanged();
    }

    private async Task ExportCsvAsync()
    {
        if (PickCsvPathAsync is null) return;
        var path = await PickCsvPathAsync();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("成员,日期,类型,分类,金额,备注");
            foreach (var row in Transactions)
            {
                sb.AppendLine($"{Escape(row.MemberName)},{row.DateText},{row.KindText},{Escape(row.CategoryName)},{row.Item.Amount:0.00},{Escape(row.Note)}");
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
            ShowStatus("已导出当前明细", false);
        }
        catch (Exception ex)
        {
            ShowStatus("导出失败：" + ex.Message, true);
        }
    }

    private void LoadMembers()
    {
        Members.Clear();
        Members.Add(new UserOptionViewModel(null, "全部成员"));
        foreach (var user in _auth.ListUsers().Where(u => u.Role == Role.Member).OrderBy(u => u.Id))
            Members.Add(new UserOptionViewModel(user.Id, user.DisplayName));
        MemberFilterIndex = 0;
    }

    private void LoadCategories()
    {
        var selectedId = CategoryFilterIndex >= 0 && CategoryFilterIndex < Categories.Count
            ? Categories[CategoryFilterIndex].Id
            : null;
        var kind = KindFilterIndex switch { 1 => TxKind.Income, 2 => TxKind.Expense, _ => (TxKind?)null };

        Categories.Clear();
        Categories.Add(new CategoryOptionViewModel(null, "全部分类"));
        foreach (var category in _ledger.ListAllCategories(kind).GroupBy(c => new { c.Id, c.Name }).Select(g => g.First()))
            Categories.Add(new CategoryOptionViewModel(category.Id, category.Name));

        var selectedIndex = selectedId is null ? 0 : Categories.ToList().FindIndex(c => c.Id == selectedId);
        CategoryFilterIndex = selectedIndex >= 0 ? selectedIndex : 0;
    }

    public void ShowStatus(string message, bool isError)
    {
        _isStatusError = isError;
        RaisePropertyChanged(nameof(StatusBrush));
        StatusMessage = message;
    }

    private static string Escape(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
