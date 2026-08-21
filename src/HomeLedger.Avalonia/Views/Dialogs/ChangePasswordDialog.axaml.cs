using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HomeLedger.Avalonia.Views.Dialogs;

public partial class ChangePasswordDialog : Window
{
    public ChangePasswordDialog()
    {
        InitializeComponent();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        var pwd = NewPasswordBox.Text ?? "";
        if (pwd.Length < 6)
        {
            ShowError("密码长度至少 6 位");
            return;
        }
        if (pwd != ConfirmBox.Text)
        {
            ShowError("两次输入的密码不一致");
            return;
        }
        AppServices.Auth.ChangePassword(Session.Current!.Id, pwd);
        Close(true);
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
