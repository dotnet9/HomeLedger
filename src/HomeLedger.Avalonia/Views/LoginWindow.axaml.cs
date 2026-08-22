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
        LeftContent = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 9,
            Margin = new Thickness(16, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new Border
                {
                    Width = 26,
                    Height = 26,
                    CornerRadius = new CornerRadius(9),
                    Background = Brush.Parse("#147d64"),
                    Child = new TextBlock
                    {
                        Text = "¥",
                        FontSize = 14,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                },
                new TextBlock
                {
                    Text = "HomeLedger 家庭记账",
                    FontSize = 14,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brush.Parse("#2b2f2c"),
                    VerticalAlignment = VerticalAlignment.Center,
                },
            },
        };
    }

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
            user.MustChangePassword = false;
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
