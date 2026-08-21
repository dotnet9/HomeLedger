using Avalonia;
using HomeLedger.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;

namespace HomeLedger.Avalonia.Views.Dialogs;

public partial class ChangePasswordDialog : JadeWindow
{
    public ChangePasswordDialog()
    {
        InitializeComponent();
        LeftContent = new TextBlock { Text = "修改密码", FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#2b2f2c"), Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
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
