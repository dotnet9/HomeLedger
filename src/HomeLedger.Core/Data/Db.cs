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
        TypeHandlers.Register();
    }

    public SqliteConnection Create() => new(_connectionString);
}

public static class TypeHandlers
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered) return;
        _registered = true;
        SqlMapper.AddTypeHandler(new DateOnlyHandler());
        SqlMapper.AddTypeHandler(new DecimalHandler());
    }

    private class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, DateOnly value) => parameter.Value = value.ToString("yyyy-MM-dd");
        public override DateOnly Parse(object value) => DateOnly.TryParse(value.ToString(), out var d) ? d : default;
    }

    private class DecimalHandler : SqlMapper.TypeHandler<decimal>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, decimal value) => parameter.Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public override decimal Parse(object value) => value switch
        {
            decimal d => d,
            double db => (decimal)db,
            long l => l,
            _ => decimal.TryParse(value.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m
        };
    }
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
            new { u = "admin", d = "管理员", h = hash, t = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
    }

    private static void SeedCategories(SqliteConnection conn)
    {
        var count = conn.ExecuteScalar<long>("SELECT COUNT(*) FROM Categories WHERE UserId IS NULL");
        if (count > 0) return;
        var items = new (string Name, TxKind Kind)[]
        {
            ("餐饮", TxKind.Expense), ("交通", TxKind.Expense), ("日常用品", TxKind.Expense),
            ("居住水电", TxKind.Expense), ("医疗", TxKind.Expense), ("教育", TxKind.Expense),
            ("娱乐", TxKind.Expense), ("人情往来", TxKind.Expense), ("服饰", TxKind.Expense),
            ("其他支出", TxKind.Expense),
            ("工资", TxKind.Income), ("奖金", TxKind.Income), ("零花钱", TxKind.Income),
            ("利息收入", TxKind.Income), ("投资收益", TxKind.Income), ("其他收入", TxKind.Income),
        }.Select((x, i) => new { Name = x.Name, Kind = (int)x.Kind, Sort = i }).ToArray();
        conn.Execute("INSERT INTO Categories(UserId, Name, Kind, SortOrder) VALUES(NULL, @Name, @Kind, @Sort)", items);
    }
}
