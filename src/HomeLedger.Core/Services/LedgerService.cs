using System.Text;
using Dapper;
using HomeLedger.Core.Data;
using HomeLedger.Core.Models;

namespace HomeLedger.Core.Services;

public class TransactionFilter
{
    public long? UserId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public TxKind? Kind { get; set; }
    public long? CategoryId { get; set; }
    public string? Keyword { get; set; }
    public int Limit { get; set; } = 500;
}

public class LedgerSummary
{
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance => Income - Expense;
}

public class LedgerService(IDbConnectionFactory db)
{
    public void AddTransaction(long userId, TxKind kind, decimal amount, DateOnly date, long? categoryId, string note)
    {
        using var conn = db.Create();
        ValidateTransaction(conn, userId, kind, amount, categoryId);
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        conn.Execute(
            "INSERT INTO Transactions(UserId, Kind, Amount, Date, CategoryId, Note, IsDeleted, CreatedAt, UpdatedAt) VALUES(@uid, @k, @a, @d, @c, @n, 0, @t, @t)",
            new { uid = userId, k = (int)kind, a = amount, d = date, c = categoryId, n = note, t = now });
    }

    public void UpdateTransaction(long userId, long id, TxKind kind, decimal amount, DateOnly date, long? categoryId, string note)
    {
        using var conn = db.Create();
        ValidateTransaction(conn, userId, kind, amount, categoryId);
        conn.Execute(
            "UPDATE Transactions SET Kind=@k, Amount=@a, Date=@d, CategoryId=@c, Note=@n, UpdatedAt=@t WHERE Id=@id AND UserId=@uid",
            new { k = (int)kind, a = amount, d = date, c = categoryId, n = note, t = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), id, uid = userId });
    }

    public void DeleteTransaction(long userId, long id)
    {
        using var conn = db.Create();
        conn.Execute("UPDATE Transactions SET IsDeleted = 1, UpdatedAt = @t WHERE Id = @id AND UserId = @uid",
            new { t = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), id, uid = userId });
    }

    public List<TransactionItem> ListTransactions(TransactionFilter f)
    {
        var sql = new StringBuilder("""
            SELECT t.Id, t.UserId, t.Kind, t.Amount, t.Date, t.CategoryId,
                   COALESCE(c.Name, '') AS CategoryName, t.Note
            FROM Transactions t LEFT JOIN Categories c ON c.Id = t.CategoryId
            WHERE t.IsDeleted = 0
            """);
        var p = new DynamicParameters();
        if (f.UserId is { } uid) { sql.Append(" AND t.UserId = @uid"); p.Add("uid", uid); }
        if (f.From is { } from) { sql.Append(" AND t.Date >= @from"); p.Add("from", from); }
        if (f.To is { } to) { sql.Append(" AND t.Date <= @to"); p.Add("to", to); }
        if (f.Kind is { } k) { sql.Append(" AND t.Kind = @k"); p.Add("k", (int)k); }
        if (f.CategoryId is { } cid) { sql.Append(" AND t.CategoryId = @cid"); p.Add("cid", cid); }
        if (!string.IsNullOrWhiteSpace(f.Keyword)) { sql.Append(" AND t.Note LIKE @kw"); p.Add("kw", $"%{f.Keyword}%"); }
        sql.Append(" ORDER BY t.Date DESC, t.Id DESC LIMIT @lim");
        p.Add("lim", f.Limit);
        using var conn = db.Create();
        return conn.Query<TransactionItem>(sql.ToString(), p).ToList();
    }

    public LedgerSummary Summary(long userId, DateOnly from, DateOnly to)
    {
        using var conn = db.Create();
        var rows = conn.Query<(int Kind, decimal Amount)>(
            "SELECT Kind, Amount FROM Transactions WHERE UserId = @uid AND IsDeleted = 0 AND Date >= @from AND Date <= @to",
            new { uid = userId, from, to }).ToList();
        return new LedgerSummary
        {
            Income = rows.Where(r => r.Kind == (int)TxKind.Income).Sum(r => r.Amount),
            Expense = rows.Where(r => r.Kind == (int)TxKind.Expense).Sum(r => r.Amount),
        };
    }

    public LedgerSummary SummaryAll(DateOnly from, DateOnly to)
    {
        using var conn = db.Create();
        var rows = conn.Query<(int Kind, decimal Amount)>(
            "SELECT Kind, Amount FROM Transactions WHERE IsDeleted = 0 AND Date >= @from AND Date <= @to",
            new { from, to }).ToList();
        return new LedgerSummary
        {
            Income = rows.Where(r => r.Kind == (int)TxKind.Income).Sum(r => r.Amount),
            Expense = rows.Where(r => r.Kind == (int)TxKind.Expense).Sum(r => r.Amount),
        };
    }

    /// <summary>按月汇总（收入/支出）。</summary>
    public List<(string Month, decimal Income, decimal Expense)> MonthlySummary(long? userId, DateOnly from, DateOnly to)
    {
        var where = "IsDeleted = 0 AND Date >= @from AND Date <= @to" + (userId is null ? "" : " AND UserId = @uid");
        using var conn = db.Create();
        return conn.Query<(string, decimal, decimal)>($"""
            SELECT substr(Date, 1, 7) AS Month,
                   SUM(CASE WHEN Kind = 1 THEN Amount ELSE 0 END),
                   SUM(CASE WHEN Kind = 0 THEN Amount ELSE 0 END)
            FROM Transactions WHERE {where}
            GROUP BY Month ORDER BY Month
            """, new { from, to, uid = userId }).ToList();
    }

    /// <summary>按分类汇总支出（或指定类型）。</summary>
    public List<(string Category, decimal Amount)> CategorySummary(long? userId, DateOnly from, DateOnly to, TxKind kind)
    {
        var where = "t.IsDeleted = 0 AND t.Date >= @from AND t.Date <= @to AND t.Kind = @k" + (userId is null ? "" : " AND t.UserId = @uid");
        using var conn = db.Create();
        return conn.Query<(string, decimal)>($"""
            SELECT COALESCE(NULLIF(c.Name, ''), '未分类') AS Cat, SUM(t.Amount)
            FROM Transactions t LEFT JOIN Categories c ON c.Id = t.CategoryId
            WHERE {where}
            GROUP BY Cat ORDER BY 2 DESC
            """, new { from, to, k = (int)kind, uid = userId }).ToList();
    }

    /// <summary>按成员汇总（管理员）。</summary>
    public List<(long UserId, string DisplayName, decimal Income, decimal Expense)> MemberSummary(DateOnly from, DateOnly to)
    {
        using var conn = db.Create();
        return conn.Query<(long, string, decimal, decimal)>("""
            SELECT u.Id, u.DisplayName,
                   SUM(CASE WHEN t.Kind = 1 THEN t.Amount ELSE 0 END),
                   SUM(CASE WHEN t.Kind = 0 THEN t.Amount ELSE 0 END)
            FROM Users u
            LEFT JOIN Transactions t ON t.UserId = u.Id AND t.IsDeleted = 0 AND t.Date >= @from AND t.Date <= @to
            WHERE u.Role = 1
            GROUP BY u.Id ORDER BY u.Id
            """, new { from, to }).ToList();
    }

    // ── 分类 ───────────────────────────
    public List<Category> ListCategories(long userId, TxKind kind)
    {
        return ListCategories(userId, (TxKind?)kind);
    }

    public List<Category> ListCategories(long userId, TxKind? kind)
    {
        using var conn = db.Create();
        var sql = new StringBuilder("SELECT * FROM Categories WHERE (UserId IS NULL OR UserId = @uid)");
        var p = new DynamicParameters();
        p.Add("uid", userId);
        if (kind is { } k)
        {
            sql.Append(" AND Kind = @k");
            p.Add("k", (int)k);
        }
        sql.Append(" ORDER BY Kind, SortOrder, Id");
        return conn.Query<Category>(sql.ToString(), p).ToList();
    }

    public List<Category> ListAllCategories(TxKind? kind = null)
    {
        using var conn = db.Create();
        var sql = new StringBuilder("SELECT * FROM Categories");
        var p = new DynamicParameters();
        if (kind is { } k)
        {
            sql.Append(" WHERE Kind = @k");
            p.Add("k", (int)k);
        }
        sql.Append(" ORDER BY Kind, SortOrder, Id");
        return conn.Query<Category>(sql.ToString(), p).ToList();
    }

    public long AddCategory(long userId, string name, TxKind kind)
    {
        using var conn = db.Create();
        return conn.ExecuteScalar<long>(
            "INSERT INTO Categories(UserId, Name, Kind, SortOrder) VALUES(@uid, @n, @k, 999); SELECT last_insert_rowid()",
            new { uid = userId, n = name, k = (int)kind });
    }

    public void DeleteCategory(long userId, long categoryId)
    {
        using var conn = db.Create();
        conn.Execute("DELETE FROM Categories WHERE Id = @id AND UserId = @uid", new { id = categoryId, uid = userId });
    }

    private static void ValidateTransaction(System.Data.IDbConnection conn, long userId, TxKind kind, decimal amount, long? categoryId)
    {
        if (amount <= 0)
            throw new InvalidOperationException("记账金额必须大于 0");

        if (categoryId is null) return;

        var category = conn.QuerySingleOrDefault<Category>(
            "SELECT * FROM Categories WHERE Id = @id",
            new { id = categoryId });
        if (category is null)
            throw new InvalidOperationException("分类不存在");
        if (category.Kind != kind)
            throw new InvalidOperationException("分类类型与收支类型不匹配");
        if (category.UserId is { } ownerId && ownerId != userId)
            throw new InvalidOperationException("不能使用其他成员的自定义分类");
    }
}
