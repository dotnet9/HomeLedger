using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HomeLedger.Avalonia.Views.Dialogs;

/// <summary>存入存款：金额、日期、年利率（默认 10%，每笔可单独设置）。</summary>
public partial class DepositDialog : Window
{
    private readonly long _userId;

    public DepositDialog(long userId)
    {
        InitializeComponent();
        _userId = userId;
        DatePicker.SelectedDate = DateTimeOffset.Now;
        RateBox.Text = "10";
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountBox.Text?.Trim(), out var amount) || amount <= 0)
        {
            ShowError("请输入正确的金额");
            return;
        }
        if (!decimal.TryParse(RateBox.Text?.Trim(), out var rate) || rate is < 0 or > 100)
        {
            ShowError("年利率需在 0 ~ 100 之间（百分数，如 10 表示 10%）");
            return;
        }
        var date = DateOnly.FromDateTime((DatePicker.SelectedDate ?? DateTimeOffset.Now).Date);
        AppServices.Deposits.AddDeposit(_userId, amount, date, rate, NoteBox.Text?.Trim() ?? "");
        Close(true);
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
