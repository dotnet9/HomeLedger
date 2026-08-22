using Avalonia;
using Avalonia.Controls;
using HomeLedger.Avalonia.Controls;
using Avalonia.Interactivity;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using Avalonia.Media;
using Avalonia.Layout;

namespace HomeLedger.Avalonia.Views.Dialogs;

public partial class TransactionDialog : JadeWindow
{
    private readonly long _userId;
    private readonly LedgerService _ledger = AppServices.Ledger;
    private readonly TransactionItem? _editing;

    public TransactionDialog()
        : this(Session.Current?.Id ?? 0)
    {
    }

    public TransactionDialog(long userId, TransactionItem? editing = null)
    {
        InitializeComponent();
        LeftContent = new TextBlock { Text = "记一笔", FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#2b2f2c"), Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        _userId = userId;
        _editing = editing;
        HeaderText.Text = editing is null ? "记一笔" : "编辑记录";
        var initDate = editing?.Date ?? DateOnly.FromDateTime(DateTime.Today);
        DatePicker.SelectedDate = new DateTimeOffset(initDate.ToDateTime(TimeOnly.MinValue));
        if (editing is not null)
        {
            AmountBox.Text = editing.Amount.ToString("0.##");
            NoteBox.Text = editing.Note;
            (editing.Kind == TxKind.Income ? IncomeRadio : ExpenseRadio).IsChecked = true;
        }
        LoadCategories();
        ExpenseRadio.IsCheckedChanged += (_, _) => LoadCategories();
        IncomeRadio.IsCheckedChanged += (_, _) => LoadCategories();
    }

    private void LoadCategories()
    {
        var kind = IncomeRadio.IsChecked == true ? TxKind.Income : TxKind.Expense;
        var categories = _ledger.ListCategories(_userId, kind);
        CategoryBox.ItemsSource = categories.Select(c => c.Name).ToList();
        if (_editing is { } e && e.Kind == kind && !string.IsNullOrEmpty(e.CategoryName))
        {
            var idx = categories.FindIndex(c => c.Name == e.CategoryName);
            if (idx >= 0) CategoryBox.SelectedIndex = idx;
        }
        else if (CategoryBox.SelectedIndex < 0 && categories.Count > 0)
            CategoryBox.SelectedIndex = 0;
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountBox.Text?.Trim(), out var amount) || amount <= 0)
        {
            ShowError("请输入正确的金额");
            return;
        }
        var kind = IncomeRadio.IsChecked == true ? TxKind.Income : TxKind.Expense;
        var date = DateOnly.FromDateTime((DatePicker.SelectedDate ?? DateTimeOffset.Now).Date);
        var categories = _ledger.ListCategories(_userId, kind);
        long? categoryId = CategoryBox.SelectedIndex >= 0 ? categories[CategoryBox.SelectedIndex].Id : null;
        var note = NoteBox.Text?.Trim() ?? "";

        if (_editing is null)
            _ledger.AddTransaction(_userId, kind, amount, date, categoryId, note);
        else
            _ledger.UpdateTransaction(_userId, _editing.Id, kind, amount, date, categoryId, note);
        Close(true);
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
