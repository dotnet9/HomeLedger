using Avalonia;
using Avalonia.Controls;
using HomeLedger.Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;

namespace HomeLedger.Avalonia.Views.Dialogs;

public partial class CreateUserDialog : JadeWindow
{
    public CreateUserDialog()
    {
        InitializeComponent();
        LeftContent = new TextBlock { Text = "新建成员账号", FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#2b2f2c"), Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        Dispatcher.UIThread.Post(() => UsernameBox.Focus());
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
        var username = UsernameBox.Text?.Trim() ?? "";
        var display = DisplayNameBox.Text?.Trim() ?? "";
        var password = PasswordBox.Text ?? "";
        if (username.Length < 2 || !username.All(char.IsLetterOrDigit))
        {
            ShowError("用户名至少 2 位，仅限字母数字");
            UsernameBox.Focus();
            UsernameBox.SelectAll();
            return;
        }
        if (string.IsNullOrEmpty(display)) { ShowError("请输入显示名"); DisplayNameBox.Focus(); return; }
        if (password.Length < 6) { ShowError("初始密码至少 6 位"); PasswordBox.Focus(); PasswordBox.SelectAll(); return; }

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

    private void UpdateConfirmState()
    {
        ConfirmButton.IsEnabled = !string.IsNullOrWhiteSpace(UsernameBox.Text)
                                  && !string.IsNullOrWhiteSpace(DisplayNameBox.Text)
                                  && !string.IsNullOrEmpty(PasswordBox.Text);
    }
}
