using CodeWF.Avalonia.Controls.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;

namespace HomeLedger.Avalonia.Views.Dialogs;

public partial class ChangePasswordDialog : CodeWFWindow
{
    public ChangePasswordDialog()
    {
        InitializeComponent();
        HomeLedger.Avalonia.Services.WindowChrome.Apply(this, Language.ChangePassword);
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
            ShowError(AppText.Get(Language.PasswordTooShort));
            NewPasswordBox.Focus();
            NewPasswordBox.SelectAll();
            return;
        }
        if (pwd != ConfirmBox.Text)
        {
            ShowError(AppText.Get(Language.PasswordMismatch));
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
