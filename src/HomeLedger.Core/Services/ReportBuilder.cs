using HomeLedger.Core.Models;

namespace HomeLedger.Core.Services;

public class ReportModel
{
    public string Title { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance => Income - Expense;
    public List<MonthlyRow> Monthly { get; set; } = [];
    public List<CategoryRow> ExpenseByCategory { get; set; } = [];
    public List<CategoryRow> IncomeByCategory { get; set; } = [];
    public List<MemberRow> Members { get; set; } = [];
    public DepositSummaryRow? Deposit { get; set; }
    public List<TransactionItem> Transactions { get; set; } = [];
}

public class MonthlyRow
{
    public string Month { get; set; } = "";
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance => Income - Expense;
}

public class CategoryRow
{
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
}

public class MemberRow
{
    public long UserId { get; set; }
    public string DisplayName { get; set; } = "";
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance => Income - Expense;
    public decimal DepositPrincipal { get; set; }
    public decimal DepositAccrued { get; set; }
    public decimal DepositTotal => DepositPrincipal + DepositAccrued;
}

public class DepositSummaryRow
{
    public decimal Principal { get; set; }
    public decimal Accrued { get; set; }
    public decimal Total => Principal + Accrued;
}

/// <summary>组装报表中间模型；userId 为 null 表示全家（管理员）。</summary>
public class ReportBuilder(LedgerService ledger, DepositService deposits)
{
    public ReportModel Build(long? userId, DateOnly from, DateOnly to, string ownerName = "")
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var model = new ReportModel
        {
            From = from,
            To = to,
            OwnerName = ownerName,
            Title = userId is null ? "全家财务报表" : $"{ownerName} 的财务报表",
        };

        var summary = userId is { } uid ? ledger.Summary(uid, from, to) : ledger.SummaryAll(from, to);
        model.Income = summary.Income;
        model.Expense = summary.Expense;
        model.Monthly = ledger.MonthlySummary(userId, from, to)
            .Select(m => new MonthlyRow { Month = m.Month, Income = m.Income, Expense = m.Expense }).ToList();
        model.ExpenseByCategory = ledger.CategorySummary(userId, from, to, TxKind.Expense)
            .Select(c => new CategoryRow { Category = c.Category, Amount = c.Amount }).ToList();
        model.IncomeByCategory = ledger.CategorySummary(userId, from, to, TxKind.Income)
            .Select(c => new CategoryRow { Category = c.Category, Amount = c.Amount }).ToList();
        model.Transactions = ledger.ListTransactions(new TransactionFilter { UserId = userId, From = from, To = to, Limit = 100000 });

        if (userId is null)
        {
            foreach (var (id, name, income, expense) in ledger.MemberSummary(from, to))
            {
                var (principal, accrued) = deposits.Totals(id, today);
                model.Members.Add(new MemberRow
                {
                    UserId = id, DisplayName = name, Income = income, Expense = expense,
                    DepositPrincipal = principal, DepositAccrued = accrued,
                });
            }
            var (p, a) = deposits.Totals(today);
            model.Deposit = new DepositSummaryRow { Principal = p, Accrued = a };
        }
        else
        {
            var (p, a) = deposits.Totals(userId!.Value, today);
            model.Deposit = new DepositSummaryRow { Principal = p, Accrued = a };
        }
        return model;
    }
}
