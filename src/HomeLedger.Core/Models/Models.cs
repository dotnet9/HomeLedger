namespace HomeLedger.Core.Models;

/// <summary>角色：管理员只管理不记账，成员记账。</summary>
public enum Role
{
    Admin = 0,
    Member = 1
}

/// <summary>收支类型。</summary>
public enum TxKind
{
    Expense = 0,
    Income = 1
}

/// <summary>存款记录类型：存入 / 支取 / 利息结转。</summary>
public enum DepositKind
{
    Deposit = 0,
    Withdrawal = 1,
    Interest = 2
}

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public string CreatedAt { get; set; } = "";
}

public class Category
{
    public long Id { get; set; }
    /// <summary>NULL 表示内置分类（只读）。</summary>
    public long? UserId { get; set; }
    public string Name { get; set; } = "";
    public TxKind Kind { get; set; }
    public int SortOrder { get; set; }
}

public class TransactionItem
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public TxKind Kind { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public long? CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Note { get; set; } = "";
    public bool IsDeleted { get; set; }
    public string CreatedAt { get; set; } = "";
}

public class DepositItem
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DepositKind Kind { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    /// <summary>年利率百分数，如 10 表示 10%。</summary>
    public decimal AnnualRate { get; set; }
    public long? SourceDepositId { get; set; }
    public long? SettlementId { get; set; }
    public string Note { get; set; } = "";
}

public class Settlement
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DateOnly SettledAt { get; set; }
    public int DepositCount { get; set; }
    public decimal TotalInterest { get; set; }
    public long CreatedBy { get; set; }
}

/// <summary>存款链视图：一笔本金 + 其结转/支取历史 + 当前应计利息。</summary>
public class DepositChainView
{
    public long HeadId { get; set; }
    public decimal Principal { get; set; }
    public decimal AnnualRate { get; set; }
    public DateOnly StartDate { get; set; }
    public decimal AccruedInterest { get; set; }
    public decimal Total => Principal + AccruedInterest;
    /// <summary>是否仍在计息（链末端未被结转/支取）。</summary>
    public bool IsActive { get; set; }
    public string ChainText { get; set; } = "";
    public DateOnly? ClosedAt { get; set; }
}
