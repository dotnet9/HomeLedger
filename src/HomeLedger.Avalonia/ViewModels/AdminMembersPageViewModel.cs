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

    public string RoleText => Item.Role == Role.Admin ? AppText.Get(Language.Administrator) : AppText.Get(Language.Member);

    public IBrush RoleBg => Item.Role == Role.Admin ? HomeLedger.Avalonia.Services.ThemeBrush.Get("StudioRaised") : HomeLedger.Avalonia.Services.ThemeBrush.Get("FinancePositiveSurface");

    public IBrush RoleFg => Item.Role == Role.Admin ? HomeLedger.Avalonia.Services.ThemeBrush.Get("StudioAccent") : HomeLedger.Avalonia.Services.ThemeBrush.Get("StudioAccent");

    public string ActiveText => Item.IsActive ? AppText.Get(Language.Enable) : AppText.Get(Language.Disabled);

    public IBrush ActiveBg => Item.IsActive ? HomeLedger.Avalonia.Services.ThemeBrush.Get("FinancePositiveSurface") : HomeLedger.Avalonia.Services.ThemeBrush.Get("StudioRaised");

    public IBrush ActiveFg => Item.IsActive ? HomeLedger.Avalonia.Services.ThemeBrush.Get("FinanceIncome") : HomeLedger.Avalonia.Services.ThemeBrush.Get("StudioMuted");

    public string ToggleText => Item.IsActive ? AppText.Get(Language.Disable) : AppText.Get(Language.Enable);

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
    private string? _keyword;
    public string? Keyword
    {
        get => _keyword;
        set { if (SetProperty(ref _keyword, value)) Refresh(); }
    }

    public ICommand CreateCommand { get; }

    public void Refresh()
    {
        Users.Clear();
        var keyword = Keyword?.Trim();
        foreach (var user in _auth.ListUsers().Where(user => string.IsNullOrEmpty(keyword) ||
                     user.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                     user.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
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
