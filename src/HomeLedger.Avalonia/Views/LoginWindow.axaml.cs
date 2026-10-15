using Avalonia;
using Avalonia.Controls;
using CodeWF.Avalonia.Controls.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;

namespace HomeLedger.Avalonia.Views;

public partial class LoginWindow : CodeWFWindow
{
    public LoginWindow()
    {
        InitializeComponent();
        HomeLedger.Avalonia.Services.WindowChrome.Apply(this, Language.AppTitle, true);
        Dispatcher.UIThread.Post(() => UsernameBox.Focus());
    }

    private void OnLoginInputChanged(object? sender, TextChangedEventArgs e)
    {
        LoginButton.IsEnabled = !string.IsNullOrWhiteSpace(UsernameBox.Text)
                                && !string.IsNullOrEmpty(PasswordBox.Text);
    }

    private void OnUsernameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !string.IsNullOrEmpty(PasswordBox.Text)) return;

        PasswordBox.Focus();
        e.Handled = true;
    }

    private async void OnLogin(object? sender, RoutedEventArgs e)
    {
        HintText.IsVisible = false;
        var username = UsernameBox.Text?.Trim();
        var password = PasswordBox.Text ?? "";
        if (string.IsNullOrEmpty(username))
        {
            ShowHint(AppText.Get(Language.UsernameRequired));
            FocusAndSelect(UsernameBox);
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            ShowHint(AppText.Get(Language.PasswordRequired));
            FocusAndSelect(PasswordBox);
            return;
        }

        var user = AppServices.Auth.Login(username, password);
        if (user == null)
        {
            ShowHint(AppText.Get(Language.LoginFailed));
            FocusAndSelect(PasswordBox);
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
                ShowHint(AppText.Get(Language.PasswordChangeRequired));
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

    private static void FocusAndSelect(TextBox textBox)
    {
        textBox.Focus();
        textBox.SelectAll();
    }
}
