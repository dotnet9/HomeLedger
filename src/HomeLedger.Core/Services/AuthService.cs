using Dapper;
using HomeLedger.Core.Data;
using HomeLedger.Core.Models;

namespace HomeLedger.Core.Services;

public class AuthService(IDbConnectionFactory db)
{
    /// <summary>登录。成功返回用户，失败返回 null。</summary>
    public User? Login(string username, string password)
    {
        using var conn = db.Create();
        var user = conn.QuerySingleOrDefault<User>(
            "SELECT * FROM Users WHERE Username = @u AND IsActive = 1", new { u = username });
        if (user == null || !PasswordHasher.Verify(password, user.PasswordHash)) return null;
        return user;
    }

    public void ChangePassword(long userId, string newPassword)
    {
        using var conn = db.Create();
        conn.Execute(
            "UPDATE Users SET PasswordHash = @h, MustChangePassword = 0 WHERE Id = @id",
            new { h = PasswordHasher.Hash(newPassword), id = userId });
    }

    /// <summary>重置成员密码，重置后首次登录强制改密。</summary>
    public void ResetPassword(long userId, string newPassword)
    {
        using var conn = db.Create();
        conn.Execute(
            "UPDATE Users SET PasswordHash = @h, MustChangePassword = 1 WHERE Id = @id",
            new { h = PasswordHasher.Hash(newPassword), id = userId });
    }

    public void CreateUser(string username, string displayName, string password)
    {
        using var conn = db.Create();
        conn.Execute(
            "INSERT INTO Users(Username, DisplayName, PasswordHash, Role, IsActive, MustChangePassword, CreatedAt) VALUES(@u, @d, @h, 1, 1, 1, @t)",
            new { u = username, d = displayName, h = PasswordHasher.Hash(password), t = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
    }

    public void SetActive(long userId, bool active)
    {
        using var conn = db.Create();
        conn.Execute("UPDATE Users SET IsActive = @a WHERE Id = @id AND Role = 1", new { a = active ? 1 : 0, id = userId });
    }

    public void Rename(long userId, string displayName)
    {
        using var conn = db.Create();
        conn.Execute("UPDATE Users SET DisplayName = @d WHERE Id = @id", new { d = displayName, id = userId });
    }

    public List<User> ListUsers()
    {
        using var conn = db.Create();
        return conn.Query<User>("SELECT * FROM Users ORDER BY Role, Id").ToList();
    }

    public List<(long Id, string DisplayName)> ListMembers()
    {
        using var conn = db.Create();
        return conn.Query<(long, string)>("SELECT Id, DisplayName FROM Users WHERE Role = 1 AND IsActive = 1 ORDER BY Id").ToList();
    }
}
