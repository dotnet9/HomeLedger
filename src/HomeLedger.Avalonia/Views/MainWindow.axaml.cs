using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using HomeLedger.Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using HomeLedger.Avalonia.Views.Pages;
using HomeLedger.Core.Models;

namespace HomeLedger.Avalonia.Views;

public partial class MainWindow : JadeWindow
{
    private readonly List<(string Title, Func<Control> Factory)> _memberPages =
    [
        ("📝 记账", () => new MemberLedgerPage()),
        ("🏦 存款利息", () => new MemberDepositsPage()),
        ("📊 我的报表", () => new MemberReportPage()),
    ];

    private readonly List<(string Title, Func<Control> Factory)> _adminPages =
    [
        ("🏠 全家总览", () => new AdminOverviewPage()),
        ("👥 成员管理", () => new AdminMembersPage()),
        ("🗄️ 数据备份", () => new AdminBackupPage()),
    ];

    public MainWindow()
    {
        InitializeComponent();
        LeftContent = new TextBlock { Text = "HomeLedger 家庭记账", FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#2b2f2c"), Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        var user = Session.Current ?? throw new InvalidOperationException("未登录");
        UserNameText.Text = user.DisplayName;
        RoleText.Text = user.Role == Role.Admin ? "管理员 · 只管理不记账" : "成员 · 只能查看自己的账";
        AvatarText.Text = user.DisplayName.Length > 0 ? user.DisplayName[^1].ToString() : "家";

        var pages = user.Role == Role.Admin ? _adminPages : _memberPages;
        foreach (var (title, _) in pages)
            NavList.Items.Add(new ListBoxItem { Content = title, Padding = new global::Avalonia.Thickness(12, 10), FontSize = 14, CornerRadius = new global::Avalonia.CornerRadius(11) });
        NavList.SelectedIndex = 0;
    }

    private void OnNavSelected(object? sender, SelectionChangedEventArgs e)
    {
        var user = Session.Current!;
        var pages = user.Role == Role.Admin ? _adminPages : _memberPages;
        if (NavList.SelectedIndex is >= 0 and var i && i < pages.Count)
            PageHost.Content = pages[i].Factory();
    }

    private async void OnChangePassword(object? sender, RoutedEventArgs e)
    {
        var dialog = new Dialogs.ChangePasswordDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        await dialog.ShowDialog<bool>(this);
    }
}
