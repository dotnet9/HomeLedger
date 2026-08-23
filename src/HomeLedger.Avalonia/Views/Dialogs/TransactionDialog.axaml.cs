using Avalonia;
using Avalonia.Controls;
using HomeLedger.Avalonia.Controls;
using Avalonia.Interactivity;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;

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
        ExpenseRadio.IsCheckedChanged += (_, _) => { LoadCategories(); ErrorText.IsVisible = false; };
        IncomeRadio.IsCheckedChanged += (_, _) => { LoadCategories(); ErrorText.IsVisible = false; };
        Dispatcher.UIThread.Post(() =>
        {
            AmountBox.Focus();
            AmountBox.SelectAll();
        });
        UpdateConfirmState();
    }

    private void LoadCategories()
    {
        var kind = IncomeRadio.IsChecked == true ? TxKind.Income : TxKind.Expense;
        var categories = _ledger.ListCategories(_userId, kind);
        var previousIndex = CategoryBox.SelectedIndex;
        CategoryBox.ItemsSource = categories.Select(c => c.Name).ToList();
        if (_editing is { } e && e.Kind == kind && !string.IsNullOrEmpty(e.CategoryName))
        {
            var idx = categories.FindIndex(c => c.Name == e.CategoryName);
            if (idx >= 0) CategoryBox.SelectedIndex = idx;
        }
        else if (previousIndex >= 0 && previousIndex < categories.Count)
            CategoryBox.SelectedIndex = previousIndex;
        else
            CategoryBox.SelectedIndex = categories.Count > 0 ? 0 : -1;
        UpdateConfirmState();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnInputChanged(object? sender, TextChangedEventArgs e)
    {
        ErrorText.IsVisible = false;
        UpdateConfirmState();
    }

    private void OnCategoryChanged(object? sender, SelectionChangedEventArgs e)
    {
        ErrorText.IsVisible = false;
        UpdateConfirmState();
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountBox.Text?.Trim(), out var amount) || amount <= 0)
        {
            ShowError("请输入正确的金额");
            AmountBox.Focus();
            AmountBox.SelectAll();
            return;
        }
        var kind = IncomeRadio.IsChecked == true ? TxKind.Income : TxKind.Expense;
        var date = DateOnly.FromDateTime((DatePicker.SelectedDate ?? DateTimeOffset.Now).Date);
        var categories = _ledger.ListCategories(_userId, kind);
        if (CategoryBox.SelectedIndex < 0 || CategoryBox.SelectedIndex >= categories.Count)
        {
            ShowError("请选择分类");
            CategoryBox.Focus();
            return;
        }
        long? categoryId = categories[CategoryBox.SelectedIndex].Id;
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

    private void UpdateConfirmState()
    {
        ConfirmButton.IsEnabled = !string.IsNullOrWhiteSpace(AmountBox.Text)
                                  && CategoryBox.SelectedIndex >= 0;
    }
}
