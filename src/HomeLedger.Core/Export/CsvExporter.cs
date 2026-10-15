using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using System.Text;

namespace HomeLedger.Core.Export;

/// <summary>报表导出（CSV）。UTF-8 BOM，Excel 可直接打开。</summary>
public static class CsvExporter
{
    public static void Export(ReportModel m, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{m.Title}（{m.OwnerName}），{m.From:yyyy-MM-dd} ~ {m.To:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine(AppText.Get(Language.IncomeExpenseSummary));
        sb.AppendLine(AppText.Format(Language.CsvIncome, m.Income));
        sb.AppendLine(AppText.Format(Language.CsvExpense, m.Expense));
        sb.AppendLine(AppText.Format(Language.CsvBalance, m.Balance));
        sb.AppendLine();
        sb.AppendLine(AppText.Get(Language.MonthlySummary));
        sb.AppendLine(AppText.Get(Language.MonthlyCsvHeader));
        foreach (var r in m.Monthly)
            sb.AppendLine($"{r.Month},{r.Income:0.00},{r.Expense:0.00},{r.Balance:0.00}");
        sb.AppendLine();
        sb.AppendLine(AppText.Get(Language.ExpenseCategories));
        sb.AppendLine(AppText.Get(Language.CategoryCsvHeader));
        foreach (var c in m.ExpenseByCategory)
            sb.AppendLine($"{c.Category},{c.Amount:0.00}");
        sb.AppendLine();
        if (m.Deposit is { } d)
        {
            sb.AppendLine(AppText.Get(Language.DepositInterest));
            sb.AppendLine(AppText.Format(Language.CsvDepositPrincipal, d.Principal));
            sb.AppendLine(AppText.Format(Language.CsvAccruedInterest, d.Accrued));
            sb.AppendLine(AppText.Format(Language.CsvDepositTotal, d.Total));
            sb.AppendLine();
        }
        if (m.Members.Count > 0)
        {
            sb.AppendLine(AppText.Get(Language.MemberSummary));
            sb.AppendLine(AppText.Get(Language.MemberCsvHeader));
            foreach (var r in m.Members)
                sb.AppendLine($"{r.DisplayName},{r.Income:0.00},{r.Expense:0.00},{r.Balance:0.00},{r.DepositPrincipal:0.00},{r.DepositAccrued:0.00},{r.DepositTotal:0.00}");
            sb.AppendLine();
        }
        sb.AppendLine(AppText.Get(Language.Details));
        sb.AppendLine(AppText.Get(Language.TransactionCsvHeader));
        foreach (var t in m.Transactions)
            sb.AppendLine($"{t.Date:yyyy-MM-dd},{(t.Kind == TxKind.Income ? AppText.Get(Language.Income) : AppText.Get(Language.Expense))},\"{t.CategoryName}\",{t.Amount:0.00},\"{t.Note.Replace("\"", "\"\"")}\"");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }
}
