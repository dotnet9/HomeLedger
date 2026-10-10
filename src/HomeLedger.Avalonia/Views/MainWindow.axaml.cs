using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using HomeLedger.Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using HomeLedger.Avalonia.ViewModels;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using CodeWF.Toolkit.Core.UpdateChecking;
using System.Diagnostics;
using System.Reflection;

namespace HomeLedger.Avalonia.Views;

public partial class MainWindow : JadeWindow
{
    public MainWindow()
    {
        InitializeComponent();
        LeftContent = new TextBlock { Text = "HomeLedger 家庭记账", FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#2b2f2c"), Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        var user = Session.Current ?? throw new InvalidOperationException("未登录");
        DataContext = new MainWindowViewModel(user);
    }

    private async void OnChangePassword(object? sender, RoutedEventArgs e)
    {
        var dialog = new Dialogs.ChangePasswordDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        await dialog.ShowDialog<bool>(this);
    }

    private static readonly UpdateChecker UpdateChecker = new("dotnet9", "HomeLedger");
    private bool _checkingUpdate;

    private async void OnCheckUpdate(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || _checkingUpdate)
        {
            return;
        }

        _checkingUpdate = true;
        var original = button.Content;
        button.IsEnabled = false;
        button.Content = "检查中…";
        try
        {
            Version? current = Assembly.GetEntryAssembly()?.GetName().Version;
            UpdateCheckResult result = await UpdateChecker.CheckAsync(current ?? new Version(0, 1, 0));
            if (!result.Succeeded)
            {
                button.Content = $"检查失败：{result.Error}";
            }
            else if (result.Update is { } update)
            {
                // 仅提醒不自动下载：提示并打开发布页
                button.Content = $"发现新版本 {update.Tag}，已打开发布页";
                Process.Start(new ProcessStartInfo(update.PageUrl) { UseShellExecute = true });
            }
            else
            {
                button.Content = "已是最新版本";
            }

            // 几秒后把按钮文案还原
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                button.Content = original;
                button.IsEnabled = true;
                _checkingUpdate = false;
            };
            timer.Start();
        }
        catch (Exception ex)
        {
            button.Content = $"检查失败：{ex.Message}";
            button.IsEnabled = true;
            _checkingUpdate = false;
        }
    }

    private void OnLogout(object? sender, RoutedEventArgs e)
    {
        Session.Current = null;
        var login = new LoginWindow { WindowStartupLocation = WindowStartupLocation.CenterScreen };
        login.Show();
        Close();
    }
}
