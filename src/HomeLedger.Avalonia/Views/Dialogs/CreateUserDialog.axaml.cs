using Avalonia;
using Avalonia.Controls;
using CodeWF.Avalonia.Controls.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;

namespace HomeLedger.Avalonia.Views.Dialogs;

public partial class CreateUserDialog : CodeWFWindow
{
    public CreateUserDialog()
    {
        InitializeComponent();
        HomeLedger.Avalonia.Services.WindowChrome.Apply(this, Language.CreateMember);
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
            ShowError(AppText.Get(Language.UsernameInvalid));
            UsernameBox.Focus();
            UsernameBox.SelectAll();
            return;
        }
        if (string.IsNullOrEmpty(display)) { ShowError(AppText.Get(Language.DisplayNameRequired)); DisplayNameBox.Focus(); return; }
        if (password.Length < 6) { ShowError(AppText.Get(Language.InitialPasswordTooShort)); PasswordBox.Focus(); PasswordBox.SelectAll(); return; }

        try
        {
            AppServices.Auth.CreateUser(username, display, password);
            Close(true);
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            ShowError(AppText.Get(Language.UsernameExists));
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
