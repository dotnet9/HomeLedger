using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Dapper;
using HomeLedger.Avalonia.Views.Dialogs;
using HomeLedger.Core.Models;

namespace HomeLedger.Avalonia.Views.Pages;

public class UserRow
{
    public required User Item { get; init; }
    public string Username => Item.Username;
    public string DisplayName => Item.DisplayName;
    public bool IsMember => Item.Role == Role.Member;
    public string RoleText => Item.Role == Role.Admin ? "管理员" : "成员";
    public IBrush RoleBg => Item.Role == Role.Admin ? SolidColorBrush.Parse("#f9efdb") : SolidColorBrush.Parse("#e3f2ec");
    public IBrush RoleFg => Item.Role == Role.Admin ? SolidColorBrush.Parse("#a97822") : SolidColorBrush.Parse("#147d64");
    public string ActiveText => Item.IsActive ? "启用" : "已停用";
    public IBrush ActiveBg => Item.IsActive ? SolidColorBrush.Parse("#e3f2ec") : SolidColorBrush.Parse("#f0eee8");
    public IBrush ActiveFg => Item.IsActive ? SolidColorBrush.Parse("#2f9e6e") : SolidColorBrush.Parse("#99a096");
    public string ToggleText => Item.IsActive ? "停用" : "启用";
    public string TxCount => Item.Role == Role.Member ? CountTx().ToString() : "—";
    public string CreatedAt => Item.CreatedAt;
    public required IRelayCommand ResetCommand { get; init; }
    public required IRelayCommand ToggleCommand { get; init; }

    private long CountTx()
    {
        using var conn = AppServices.Db.Create();
        return conn.QuerySingle<long>("SELECT COUNT(*) FROM Transactions WHERE UserId = @id AND IsDeleted = 0", new { id = Item.Id });
    }
}

public partial class AdminMembersPage : UserControl
{
    public AdminMembersPage()
    {
        InitializeComponent();
        Refresh();
    }

    private void Refresh()
    {
        ListHost.ItemsSource = AppServices.Auth.ListUsers().Select(u => new UserRow
        {
            Item = u,
            ResetCommand = new RelayCommand(() => Reset(u)),
            ToggleCommand = new RelayCommand(() => Toggle(u)),
        }).ToList();
    }

    private async void OnCreate(object? sender, RoutedEventArgs e)
    {
        var dialog = new CreateUserDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        if (await dialog.ShowDialog<bool>(Owner())) Refresh();
    }

    private void Reset(User user)
    {
        var pwd = "123456";
        AppServices.Auth.ResetPassword(user.Id, pwd);
        Refresh();
    }

    private void Toggle(User user)
    {
        AppServices.Auth.SetActive(user.Id, !user.IsActive);
        Refresh();
    }

    private Window Owner() => VisualRoot as Window ?? throw new InvalidOperationException();
}
