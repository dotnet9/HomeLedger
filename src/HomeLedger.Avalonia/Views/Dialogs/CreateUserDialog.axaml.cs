using Avalonia;
using Avalonia.Controls;
using HomeLedger.Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;

namespace HomeLedger.Avalonia.Views.Dialogs;

public partial class CreateUserDialog : JadeWindow
{
    public CreateUserDialog()
    {
        InitializeComponent();
        LeftContent = new TextBlock { Text = "新建成员账号", FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#2b2f2c"), Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        var username = UsernameBox.Text?.Trim() ?? "";
        var display = DisplayNameBox.Text?.Trim() ?? "";
        var password = PasswordBox.Text ?? "";
        if (username.Length < 2 || !username.All(char.IsLetterOrDigit))
        {
            ShowError("用户名至少 2 位，仅限字母数字");
            return;
        }
        if (string.IsNullOrEmpty(display)) { ShowError("请输入显示名"); return; }
        if (password.Length < 6) { ShowError("初始密码至少 6 位"); return; }

        try
        {
            AppServices.Auth.CreateUser(username, display, password);
            Close(true);
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            ShowError("用户名已存在");
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
