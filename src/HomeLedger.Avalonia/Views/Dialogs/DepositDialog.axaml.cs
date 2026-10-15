using Avalonia;
using Avalonia.Controls;
using CodeWF.Avalonia.Controls.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;

namespace HomeLedger.Avalonia.Views.Dialogs;

/// <summary>存入存款：金额、日期、年利率（默认 10%，每笔可单独设置）。</summary>
public partial class DepositDialog : CodeWFWindow
{
    private readonly long _userId;

    public DepositDialog()
        : this(Session.Current?.Id ?? 0)
    {
    }

    public DepositDialog(long userId)
    {
        InitializeComponent();
        HomeLedger.Avalonia.Services.WindowChrome.Apply(this, Language.AddDeposit);
        _userId = userId;
        DatePicker.SelectedDate = DateTimeOffset.Now;
        RateBox.Text = "10";
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
            ShowError(AppText.Get(Language.AmountInvalid));
            AmountBox.Focus();
            AmountBox.SelectAll();
            return;
        }
        if (!decimal.TryParse(RateBox.Text?.Trim(), out var rate) || rate is < 0 or > 100)
        {
            ShowError(AppText.Get(Language.RateInvalid));
            RateBox.Focus();
            RateBox.SelectAll();
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

    private void UpdateConfirmState()
    {
        ConfirmButton.IsEnabled = !string.IsNullOrWhiteSpace(AmountBox.Text)
                                  && !string.IsNullOrWhiteSpace(RateBox.Text);
    }
}
