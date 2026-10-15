using Microsoft.Data.Sqlite;
using System.Text;
using Dapper;
using HomeLedger.Core.Models;

namespace HomeLedger.Core.Data;

/// <summary>数据库连接工厂。数据库文件为 SQLite 单文件。</summary>
public interface IDbConnectionFactory
{
    SqliteConnection Create();
}

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
    }

    public SqliteConnection Create() => new(_connectionString);
}

/// <summary>建库、建表与种子数据。</summary>
public static class Db
{
    public static void Initialize(IDbConnectionFactory factory)
    {
        using var conn = factory.Create();
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                DisplayName TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                Role INTEGER NOT NULL DEFAULT 1,
                IsActive INTEGER NOT NULL DEFAULT 1,
                MustChangePassword INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NULL,
                Name TEXT NOT NULL,
                Kind INTEGER NOT NULL,
                SortOrder INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Kind INTEGER NOT NULL,
                Amount TEXT NOT NULL,
                Date TEXT NOT NULL,
                CategoryId INTEGER NULL,
                Note TEXT NOT NULL DEFAULT '',
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Deposits (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Kind INTEGER NOT NULL,
                Amount TEXT NOT NULL,
                Date TEXT NOT NULL,
                AnnualRate TEXT NOT NULL DEFAULT '10',
                SourceDepositId INTEGER NULL,
                SettlementId INTEGER NULL,
                Note TEXT NOT NULL DEFAULT ''
            );
            CREATE TABLE IF NOT EXISTS InterestSettlements (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                SettledAt TEXT NOT NULL,
                DepositCount INTEGER NOT NULL,
                TotalInterest TEXT NOT NULL,
                CreatedBy INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_tx_user_date ON Transactions(UserId, Date);
            CREATE INDEX IF NOT EXISTS idx_dep_user ON Deposits(UserId);
            """);

        SeedUsers(conn);
        SeedCategories(conn);
    }

    private static void SeedUsers(SqliteConnection conn)
    {
        var count = conn.ExecuteScalar<long>("SELECT COUNT(*) FROM Users");
        if (count > 0) return;
        var hash = PasswordHasher.Hash("admin123");
        conn.Execute(
            "INSERT INTO Users(Username, DisplayName, PasswordHash, Role, IsActive, MustChangePassword, CreatedAt) VALUES(@u, @d, @h, 0, 1, 1, @t)",
            new { u = "admin", d = AppText.Invariant(Language.Administrator), h = hash, t = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
    }

    private static void SeedCategories(SqliteConnection conn)
    {
        var count = conn.ExecuteScalar<long>("SELECT COUNT(*) FROM Categories WHERE UserId IS NULL");
        if (count > 0) return;
        var items = new (string Name, TxKind Kind)[]
        {
            (AppText.Invariant(Language.CategoryFood), TxKind.Expense), (AppText.Invariant(Language.CategoryTransport), TxKind.Expense), (AppText.Invariant(Language.CategoryDaily), TxKind.Expense),
            (AppText.Invariant(Language.CategoryHousing), TxKind.Expense), (AppText.Invariant(Language.CategoryMedical), TxKind.Expense), (AppText.Invariant(Language.CategoryEducation), TxKind.Expense),
            (AppText.Invariant(Language.CategoryEntertainment), TxKind.Expense), (AppText.Invariant(Language.CategoryGifts), TxKind.Expense), (AppText.Invariant(Language.CategoryClothing), TxKind.Expense),
            (AppText.Invariant(Language.CategoryOtherExpense), TxKind.Expense),
            (AppText.Invariant(Language.CategorySalary), TxKind.Income), (AppText.Invariant(Language.CategoryBonus), TxKind.Income), (AppText.Invariant(Language.CategoryAllowance), TxKind.Income),
            (AppText.Invariant(Language.CategoryInterest), TxKind.Income), (AppText.Invariant(Language.CategoryInvestment), TxKind.Income), (AppText.Invariant(Language.CategoryOtherIncome), TxKind.Income),
        }.Select((x, i) => new { Name = x.Name, Kind = (int)x.Kind, Sort = i }).ToArray();
        conn.Execute("INSERT INTO Categories(UserId, Name, Kind, SortOrder) VALUES(NULL, @Name, @Kind, @Sort)", items);
    }
}
