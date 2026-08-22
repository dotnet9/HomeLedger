using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
using Dapper;
using HomeLedger.Avalonia.Commands;
using HomeLedger.Core.Data;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace HomeLedger.Avalonia.ViewModels;

public sealed class UserRowViewModel
{
    private readonly IDbConnectionFactory _db;

    public UserRowViewModel(User item, IDbConnectionFactory db, Action<User> reset, Action<User> toggle)
    {
        Item = item;
        _db = db;
        ResetCommand = new DelegateCommand(() => reset(Item));
        ToggleCommand = new DelegateCommand(() => toggle(Item));
    }

    public User Item { get; }

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

    public string TxCount => Item.Role == Role.Member ? CountTransactions().ToString() : "—";

    public string CreatedAt => Item.CreatedAt;

    public ICommand ResetCommand { get; }

    public ICommand ToggleCommand { get; }

    private long CountTransactions()
    {
        using var conn = _db.Create();
        return conn.QuerySingle<long>("SELECT COUNT(*) FROM Transactions WHERE UserId = @id AND IsDeleted = 0", new { id = Item.Id });
    }
}

public sealed class AdminMembersPageViewModel : BindableBase
{
    private readonly AuthService _auth;
    private readonly IDbConnectionFactory _db;

    public AdminMembersPageViewModel(AuthService auth, IDbConnectionFactory db)
    {
        _auth = auth;
        _db = db;
        CreateCommand = new AsyncDelegateCommand(CreateAsync);
        Refresh();
    }

    public Func<Task<bool>>? ShowCreateUserDialogAsync { get; set; }

    public ObservableCollection<UserRowViewModel> Users { get; } = [];

    public ICommand CreateCommand { get; }

    public void Refresh()
    {
        Users.Clear();
        foreach (var user in _auth.ListUsers())
            Users.Add(new UserRowViewModel(user, _db, Reset, Toggle));
    }

    private async Task CreateAsync()
    {
        if (ShowCreateUserDialogAsync is null) return;
        if (await ShowCreateUserDialogAsync()) Refresh();
    }

    private void Reset(User user)
    {
        _auth.ResetPassword(user.Id, "123456");
        Refresh();
    }

    private void Toggle(User user)
    {
        _auth.SetActive(user.Id, !user.IsActive);
        Refresh();
    }
}
