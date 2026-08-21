using Avalonia.Controls;
using Avalonia.Interactivity;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;

namespace HomeLedger.Avalonia.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private User? _pendingUser;

    private async void OnLogin(object? sender, RoutedEventArgs e)
    {
        HintText.IsVisible = false;
        var username = UsernameBox.Text?.Trim();
        var password = PasswordBox.Text ?? "";
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowHint("请输入用户名和密码");
            return;
        }

        var user = AppServices.Auth.Login(username, password);
        if (user == null)
        {
            ShowHint("用户名或密码错误，或账号已停用");
            return;
        }

        if (user.MustChangePassword)
        {
            var dialog = new Dialogs.ChangePasswordDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
            if (await dialog.ShowDialog<bool>(this) != true)
            {
                ShowHint("请先完成密码修改");
                return;
            }
            user = AppServices.Auth.Login(username, password)!;
        }

        Session.Current = user;
        var main = new MainWindow();
        main.Show();
        Close();
    }

    private void ShowHint(string message)
    {
        HintText.Text = message;
        HintText.IsVisible = true;
    }
}
