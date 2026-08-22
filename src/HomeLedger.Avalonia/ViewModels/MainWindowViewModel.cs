using Avalonia.Controls;
using HomeLedger.Avalonia.Views.Pages;
using HomeLedger.Core.Models;
using Prism.Mvvm;

namespace HomeLedger.Avalonia.ViewModels;

public sealed class NavigationItemViewModel
{
    private readonly Func<Control> _factory;

    public NavigationItemViewModel(string title, Func<Control> factory)
    {
        Title = title;
        _factory = factory;
    }

    public string Title { get; }

    public Control CreatePage() => _factory();
}

public sealed class MainWindowViewModel : BindableBase
{
    private int _selectedPageIndex;
    private Control? _currentPage;

    public MainWindowViewModel(User user)
    {
        UserName = user.DisplayName;
        RoleText = user.Role == Role.Admin ? "管理员 · 只管理不记账" : "成员 · 只能查看自己的账";
        AvatarText = user.DisplayName.Length > 0 ? user.DisplayName[^1].ToString() : "家";
        Pages = user.Role == Role.Admin ? CreateAdminPages() : CreateMemberPages();

        _selectedPageIndex = 0;
        _currentPage = Pages.Count > 0 ? Pages[0].CreatePage() : null;
    }

    public IReadOnlyList<NavigationItemViewModel> Pages { get; }

    public string UserName { get; }

    public string RoleText { get; }

    public string AvatarText { get; }

    public int SelectedPageIndex
    {
        get => _selectedPageIndex;
        set
        {
            if (!SetProperty(ref _selectedPageIndex, value)) return;
            CurrentPage = value >= 0 && value < Pages.Count ? Pages[value].CreatePage() : null;
        }
    }

    public Control? CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    private static IReadOnlyList<NavigationItemViewModel> CreateMemberPages() =>
    [
        new("📝 记账", () => new MemberLedgerPage()),
        new("🏦 存款利息", () => new MemberDepositsPage()),
        new("📊 我的报表", () => new MemberReportPage()),
    ];

    private static IReadOnlyList<NavigationItemViewModel> CreateAdminPages() =>
    [
        new("🏠 全家总览", () => new AdminOverviewPage()),
        new("👥 成员管理", () => new AdminMembersPage()),
        new("🗄️ 数据备份", () => new AdminBackupPage()),
    ];
}
