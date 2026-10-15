using Avalonia.Controls;
using Avalonia.Threading;
using HomeLedger.Avalonia.Views.Pages;
using HomeLedger.Core.Models;
using Prism.Mvvm;
using CodeWF.Toolkit.Lang;

namespace HomeLedger.Avalonia.ViewModels;

public sealed class NavigationItemViewModel : BindableBase
{
    private readonly Func<Control> _factory;

    public NavigationItemViewModel(string title, Func<Control> factory)
    {
        _titleKey = title;
        _factory = factory;
    }

    private readonly string _titleKey;
    public string Title => AppText.Get(_titleKey);
    public void Refresh() => RaisePropertyChanged(nameof(Title));

    public Control CreatePage() => _factory();
}

public sealed class MainWindowViewModel : BindableBase, IDisposable
{
    private int _selectedPageIndex;
    private Control? _currentPage;
    private readonly User _user;
    private bool _disposed;

    public MainWindowViewModel(User user)
    {
        _user = user;
        UserName = user.DisplayName;
        RoleText = user.Role == Role.Admin ? AppText.Get(Language.AdministratorRole) : AppText.Get(Language.MemberRole);
        AvatarText = user.DisplayName.Length > 0 ? user.DisplayName[^1].ToString() : AppText.Get(Language.AvatarFallback);
        Pages = user.Role == Role.Admin ? CreateAdminPages() : CreateMemberPages();

        _selectedPageIndex = 0;
        _currentPage = Pages.Count > 0 ? Pages[0].CreatePage() : null;
        I18nManager.Instance.CultureChanged += OnCultureChanged;
        if (global::Avalonia.Application.Current is { } app) app.ActualThemeVariantChanged += OnCultureChanged;
    }

    public IReadOnlyList<NavigationItemViewModel> Pages { get; private set; }

    public string UserName { get; }

    public string RoleText { get; private set; }

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
        new(Language.NavigationLedger, () => new MemberLedgerPage()),
        new(Language.NavigationDeposits, () => new MemberDepositsPage()),
        new(Language.NavigationReports, () => new MemberReportPage()),
    ];

    private static IReadOnlyList<NavigationItemViewModel> CreateAdminPages() =>
    [
        new(Language.NavigationOverview, () => new AdminOverviewPage()),
        new(Language.NavigationDetails, () => new AdminDetailsPage()),
        new(Language.NavigationMembers, () => new AdminMembersPage()),
        new(Language.NavigationBackup, () => new AdminBackupPage()),
    ];

    private void OnCultureChanged(object? sender, EventArgs args)
    {
        if (_disposed) return;
        if (!Dispatcher.UIThread.CheckAccess()) { Dispatcher.UIThread.Post(() => OnCultureChanged(sender, args)); return; }
        RoleText = AppText.Get(_user.Role == Role.Admin ? Language.AdministratorRole : Language.MemberRole);
        RaisePropertyChanged(nameof(RoleText));
        foreach (var page in Pages) page.Refresh();
        switch (CurrentPage?.DataContext)
        {
            case MemberLedgerPageViewModel ledger: ledger.RefreshLocalization(); break;
            case MemberDepositsPageViewModel deposits: deposits.Refresh(); break;
            case AdminDetailsPageViewModel details: details.RefreshLocalization(); break;
            case AdminMembersPageViewModel members: members.Refresh(); break;
        }
        if (CurrentPage is MemberReportPage report) report.Refresh();
        if (CurrentPage is AdminOverviewPage overview) overview.Refresh();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        I18nManager.Instance.CultureChanged -= OnCultureChanged;
        if (global::Avalonia.Application.Current is { } app) app.ActualThemeVariantChanged -= OnCultureChanged;
    }
}
