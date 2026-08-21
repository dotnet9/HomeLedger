using Avalonia;
using Avalonia.Controls;
using HomeLedger.Avalonia.Controls;
using Avalonia.Interactivity;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using Avalonia.Media;
using Avalonia.Layout;

namespace HomeLedger.Avalonia.Views;

public partial class LoginWindow : JadeWindow
{
    public LoginWindow()
    {
        InitializeComponent();
        LeftContent = new TextBlock { Text = "HomeLedger 家庭记账", FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#2b2f2c"), Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
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
            // 先建立会话：改密对话框与后续流程都依赖 Session.Current
            Session.Current = user;
            var dialog = new Dialogs.ChangePasswordDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
            if (await dialog.ShowDialog<bool>(this) != true)
            {
                Session.Current = null;
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
