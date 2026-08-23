using Avalonia;
using Avalonia.Controls;
using HomeLedger.Avalonia.Controls;
using Avalonia.Interactivity;
using HomeLedger.Core.Models;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;

namespace HomeLedger.Avalonia.Views.Dialogs;

/// <summary>支取：整链结清或部分支取，剩余本息按原利率续存。</summary>
public partial class WithdrawDialog : JadeWindow
{
    private readonly long _userId;
    private readonly DepositChainView _chain;

    public WithdrawDialog()
        : this(Session.Current?.Id ?? 0, new DepositChainView { StartDate = DateOnly.FromDateTime(DateTime.Today) })
    {
    }

    public WithdrawDialog(long userId, DepositChainView chain)
    {
        InitializeComponent();
        LeftContent = new TextBlock { Text = "支取存款", FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#2b2f2c"), Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        _userId = userId;
        _chain = chain;
        DatePicker.SelectedDate = DateTimeOffset.Now;
        InfoText.Text = $"存款 {chain.Principal:0.00}（{chain.AnnualRate:0.##}%，起息 {chain.StartDate:yyyy-MM-dd}），"
            + $"应计利息 {chain.AccruedInterest:0.00}，本息合计 {chain.Total:0.00}";
        MaxText.Text = $"最多可支取：{chain.Total:0.00}";
        AmountBox.Text = chain.Total.ToString("0.##");
        Dispatcher.UIThread.Post(() =>
        {
            AmountBox.Focus();
            AmountBox.SelectAll();
        });
        UpdateConfirmState();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnInputChanged(object? sender, TextChangedEventArgs e)
    {
        ErrorText.IsVisible = false;
        UpdateConfirmState();
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountBox.Text?.Trim(), out var amount) || amount <= 0)
        {
            ShowError("请输入正确的支取金额");
            AmountBox.Focus();
            AmountBox.SelectAll();
            return;
        }
        try
        {
            var date = DateOnly.FromDateTime((DatePicker.SelectedDate ?? DateTimeOffset.Now).Date);
            AppServices.Deposits.Withdraw(_userId, _chain.HeadId, amount, date, NoteBox.Text?.Trim() ?? "");
            Close(true);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    private void UpdateConfirmState()
    {
        ConfirmButton.IsEnabled = !string.IsNullOrWhiteSpace(AmountBox.Text);
    }
}
