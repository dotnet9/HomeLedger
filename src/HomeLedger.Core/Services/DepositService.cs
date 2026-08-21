using Microsoft.Data.Sqlite;
using Dapper;
using HomeLedger.Core.Data;
using HomeLedger.Core.Models;

namespace HomeLedger.Core.Services;

/// <summary>
/// 家庭存款与利息。模型：每笔存款形成一条链（存入 → 结转利息 → 结转利息… → 支取），
/// 只有链末端记录继续起息；结转/支取后原记录因有子记录而停息。
/// </summary>
public class DepositService(IDbConnectionFactory db)
{
    public long AddDeposit(long userId, decimal amount, DateOnly date, decimal annualRatePercent, string note)
    {
        if (amount <= 0) throw new InvalidOperationException("存款金额必须大于 0");
        using var conn = db.Create();
        return conn.ExecuteScalar<long>("""
            INSERT INTO Deposits(UserId, Kind, Amount, Date, AnnualRate, Note)
            VALUES(@uid, 0, @a, @d, @r, @n); SELECT last_insert_rowid()
            """, new { uid = userId, a = amount, d = date, r = annualRatePercent, n = note });
    }

    /// <summary>支取：整链结清或部分支取。部分支取时，剩余本息生成新存款（沿用原利率）继续起息。</summary>
    public void Withdraw(long userId, long headId, decimal amount, DateOnly date, string note)
    {
        using var conn = db.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var head = GetActiveHead(conn, tx, userId, headId);
            var interest = InterestCalculator.Accrued(head.Amount, head.AnnualRate, head.Date, date);
            var proceed = head.Amount + interest;
            if (amount <= 0 || amount > proceed) throw new InvalidOperationException($"支取金额需在 0 ~ {proceed} 之间");

            conn.Execute(
                "INSERT INTO Deposits(UserId, Kind, Amount, Date, AnnualRate, SourceDepositId, Note) VALUES(@uid, 1, @a, @d, @r, @src, @n)",
                new { uid = userId, a = amount, d = date, r = head.AnnualRate, src = headId, n = string.IsNullOrEmpty(note) ? "支取" : note }, tx);

            var remaining = proceed - amount;
            if (remaining > 0)
                conn.Execute(
                    "INSERT INTO Deposits(UserId, Kind, Amount, Date, AnnualRate, Note) VALUES(@uid, 0, @a, @d, @r, @n)",
                    new { uid = userId, a = Math.Round(remaining, 2), d = date, r = head.AnnualRate, n = "支取后剩余本息续存" }, tx);

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    /// <summary>结算：将选中链的应计利息结转为新本金（复利），并记录结算历史。</summary>
    public Settlement Settle(long userId, IReadOnlyList<long> headIds, DateOnly settleDate, long createdBy)
    {
        using var conn = db.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            decimal total = 0;
            var count = 0;
            foreach (var headId in headIds)
            {
                var head = GetActiveHead(conn, tx, userId, headId);
                var interest = InterestCalculator.Accrued(head.Amount, head.AnnualRate, head.Date, settleDate);
                if (interest <= 0) continue;
                total += interest;
                count++;
                conn.Execute(
                    "INSERT INTO Deposits(UserId, Kind, Amount, Date, AnnualRate, SourceDepositId, Note) VALUES(@uid, 2, @a, @d, @r, @src, @n)",
                    new { uid = userId, a = interest, d = settleDate, r = head.AnnualRate, src = headId, n = "利息结转本金" }, tx);
            }

            var settlementId = conn.ExecuteScalar<long>(
                "INSERT INTO InterestSettlements(UserId, SettledAt, DepositCount, TotalInterest, CreatedBy) VALUES(@uid, @d, @c, @t, @by); SELECT last_insert_rowid()",
                new { uid = userId, d = settleDate, c = count, t = Math.Round(total, 2), by = createdBy }, tx);
            conn.Execute("UPDATE Deposits SET SettlementId = @s WHERE Kind = 2 AND SettlementId IS NULL AND Id IN @ids",
                new { s = settlementId, ids = conn.Query<long>("SELECT Id FROM Deposits WHERE Kind = 2 AND SettlementId IS NULL AND UserId = @uid", new { uid = userId }, tx) }, tx);

            tx.Commit();
            return new Settlement { Id = settlementId, UserId = userId, SettledAt = settleDate, DepositCount = count, TotalInterest = Math.Round(total, 2), CreatedBy = createdBy };
        }
        catch { tx.Rollback(); throw; }
    }

    /// <summary>列出成员所有存款链（含历史与计息中）。</summary>
    public List<DepositChainView> ListChains(long userId, DateOnly today)
    {
        using var conn = db.Create();
        var rows = conn.Query<DepositItem>(
            "SELECT * FROM Deposits WHERE UserId = @uid ORDER BY Date, Id", new { uid = userId }).ToList();
        return BuildChains(rows, today);
    }

    /// <summary>管理员视角：全员存款汇总（本金合计 / 应计利息合计 / 本息合计）。</summary>
    public (decimal Principal, decimal Accrued) Totals(DateOnly today)
    {
        using var conn = db.Create();
        var users = conn.Query<long>("SELECT Id FROM Users WHERE Role = 1").ToList();
        decimal principal = 0, accrued = 0;
        foreach (var uid in users)
            foreach (var chain in ListChains(conn, uid, today).Where(c => c.IsActive))
            {
                principal += chain.Principal;
                accrued += chain.AccruedInterest;
            }
        return (principal, accrued);
    }

    public (decimal Principal, decimal Accrued) Totals(long userId, DateOnly today)
    {
        using var conn = db.Create();
        return ListChains(conn, userId, today).Where(c => c.IsActive)
            .Aggregate((0m, 0m), (acc, c) => (acc.Item1 + c.Principal, acc.Item2 + c.AccruedInterest));
    }

    public List<Settlement> ListSettlements(long userId)
    {
        using var conn = db.Create();
        return conn.Query<Settlement>(
            "SELECT s.*, u.DisplayName FROM InterestSettlements s LEFT JOIN Users u ON u.Id = s.CreatedBy WHERE s.UserId = @uid ORDER BY s.SettledAt DESC",
            new { uid = userId }).ToList();
    }

    internal static List<DepositChainView> BuildChains(List<DepositItem> rows, DateOnly today)
    {
        var children = rows.Where(r => r.SourceDepositId is not null)
                           .GroupBy(r => r.SourceDepositId!.Value)
                           .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Date).ThenBy(r => r.Id).Last());
        var result = new List<DepositChainView>();
        foreach (var row in rows.Where(r => r.Kind != DepositKind.Withdrawal))
        {
            var hasChild = children.ContainsKey(row.Id);
            var view = new DepositChainView
            {
                HeadId = row.Id,
                Principal = row.Amount,
                AnnualRate = row.AnnualRate,
                StartDate = row.Date,
                IsActive = !hasChild,
                AccruedInterest = hasChild ? 0 : InterestCalculator.Accrued(row.Amount, row.AnnualRate, row.Date, today),
            };
            // 沿链溯源生成可读文本
            var parts = new List<string> { row.Kind switch {
                DepositKind.Deposit => $"存入 {row.Amount:0.##}",
                _ => $"{(row.Kind == DepositKind.Interest ? "结转" : "?")} {row.Amount:0.##}（{row.Date:MM-dd}）" } };
            var child = rows.FirstOrDefault(r => r.SourceDepositId == row.Id);
            while (child is not null)
            {
                parts.Add(child.Kind == DepositKind.Withdrawal
                    ? $"支取 {child.Amount:0.##}（{child.Date:MM-dd}）"
                    : $"结转 {child.Amount:0.##}（{child.Date:MM-dd}）");
                child = rows.FirstOrDefault(r => r.SourceDepositId == child.Id);
            }
            view.ChainText = string.Join(" → ", parts);
            if (!view.IsActive)
                view.ClosedAt = children[row.Id].Date;
            result.Add(view);
        }
        return result.OrderByDescending(c => c.IsActive).ThenBy(c => c.StartDate).ToList();
    }

    private static DepositItem GetActiveHead(SqliteConnection conn, SqliteTransaction? tx, long userId, long headId)
    {
        var head = conn.QuerySingleOrDefault<DepositItem>(
            "SELECT * FROM Deposits WHERE Id = @id AND UserId = @uid AND Kind != 1", new { id = headId, uid = userId }, tx)
            ?? throw new InvalidOperationException("存款记录不存在");
        var hasChild = conn.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM Deposits WHERE SourceDepositId = @id", new { id = headId }, tx) > 0;
        if (hasChild) throw new InvalidOperationException("该存款已结转或支取，不能重复操作");
        return head;
    }

    private static List<DepositChainView> ListChains(SqliteConnection conn, long userId, DateOnly today)
    {
        var rows = conn.Query<DepositItem>("SELECT * FROM Deposits WHERE UserId = @uid ORDER BY Date, Id", new { uid = userId }).ToList();
        return BuildChains(rows, today);
    }
}
