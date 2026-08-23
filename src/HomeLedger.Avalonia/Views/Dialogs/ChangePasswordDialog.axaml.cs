using HomeLedger.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;

namespace HomeLedger.Avalonia.Views.Dialogs;

public partial class ChangePasswordDialog : JadeWindow
{
    public ChangePasswordDialog()
    {
        InitializeComponent();
        LeftContent = new TextBlock { Text = "修改密码", FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#2b2f2c"), Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        Dispatcher.UIThread.Post(() => NewPasswordBox.Focus());
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
        var pwd = NewPasswordBox.Text ?? "";
        if (pwd.Length < 6)
        {
            ShowError("密码长度至少 6 位");
            NewPasswordBox.Focus();
            NewPasswordBox.SelectAll();
            return;
        }
        if (pwd != ConfirmBox.Text)
        {
            ShowError("两次输入的密码不一致");
            ConfirmBox.Focus();
            ConfirmBox.SelectAll();
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

    private void UpdateConfirmState()
    {
        ConfirmButton.IsEnabled = !string.IsNullOrEmpty(NewPasswordBox.Text)
                                  && !string.IsNullOrEmpty(ConfirmBox.Text);
    }
}
