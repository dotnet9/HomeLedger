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
        sb.AppendLine("收支汇总");
        sb.AppendLine($"收入,{m.Income:0.00}");
        sb.AppendLine($"支出,{m.Expense:0.00}");
        sb.AppendLine($"结余,{m.Balance:0.00}");
        sb.AppendLine();
        sb.AppendLine("月度汇总");
        sb.AppendLine("月份,收入,支出,结余");
        foreach (var r in m.Monthly)
            sb.AppendLine($"{r.Month},{r.Income:0.00},{r.Expense:0.00},{r.Balance:0.00}");
        sb.AppendLine();
        sb.AppendLine("支出分类");
        sb.AppendLine("分类,金额");
        foreach (var c in m.ExpenseByCategory)
            sb.AppendLine($"{c.Category},{c.Amount:0.00}");
        sb.AppendLine();
        if (m.Deposit is { } d)
        {
            sb.AppendLine("存款利息");
            sb.AppendLine($"存款本金合计,{d.Principal:0.00}");
            sb.AppendLine($"当前应计利息,{d.Accrued:0.00}");
            sb.AppendLine($"本息合计,{d.Total:0.00}");
            sb.AppendLine();
        }
        if (m.Members.Count > 0)
        {
            sb.AppendLine("成员汇总");
            sb.AppendLine("成员,收入,支出,结余,存款本金,应计利息,本息合计");
            foreach (var r in m.Members)
                sb.AppendLine($"{r.DisplayName},{r.Income:0.00},{r.Expense:0.00},{r.Balance:0.00},{r.DepositPrincipal:0.00},{r.DepositAccrued:0.00},{r.DepositTotal:0.00}");
            sb.AppendLine();
        }
        sb.AppendLine("明细");
        sb.AppendLine("日期,类型,分类,金额,备注");
        foreach (var t in m.Transactions)
            sb.AppendLine($"{t.Date:yyyy-MM-dd},{(t.Kind == TxKind.Income ? "收入" : "支出")},\"{t.CategoryName}\",{t.Amount:0.00},\"{t.Note.Replace("\"", "\"\"")}\"");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }
}
