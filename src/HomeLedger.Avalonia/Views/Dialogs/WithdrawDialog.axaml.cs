using Avalonia;
using Avalonia.Controls;
using CodeWF.Avalonia.Controls.Controls;
using Avalonia.Interactivity;
using HomeLedger.Core.Models;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;

namespace HomeLedger.Avalonia.Views.Dialogs;

/// <summary>支取：整链结清或部分支取，剩余本息按原利率续存。</summary>
public partial class WithdrawDialog : CodeWFWindow
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
        HomeLedger.Avalonia.Services.WindowChrome.Apply(this, Language.WithdrawDeposit);
        _userId = userId;
        _chain = chain;
        DatePicker.SelectedDate = DateTimeOffset.Now;
        InfoText.Text = AppText.Format(Language.WithdrawalDetails, chain.Principal, chain.AnnualRate, chain.StartDate)
            + AppText.Format(Language.WithdrawalTotals, chain.AccruedInterest, chain.Total);
        MaxText.Text = AppText.Format(Language.WithdrawalMaximum, chain.Total);
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
            ShowError(AppText.Get(Language.WithdrawalInvalid));
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
