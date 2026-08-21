using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HomeLedger.Core.Export;

/// <summary>报表导出（PDF）：封面 + 汇总 + 图表 + 表格。QuestPDF 社区授权（家庭自用符合 Community 条款）。</summary>
public static class PdfExporter
{
    static PdfExporter() => QuestPDF.Settings.License = LicenseType.Community;

    public static void Export(ReportModel m, string path)
    {
        var barPng = ChartImageRenderer.MonthlyBars(m.Monthly.Select(r => (r.Month, r.Income, r.Expense)).ToList());
        var donutPng = ChartImageRenderer.Donut(m.ExpenseByCategory.Select(c => (c.Category, c.Amount)).ToList());

        Document.Create(container => container.Page(page =>
        {
            page.Margin(28);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.SegoeUI));

            page.Header().Column(c =>
            {
                c.Item().Text(m.Title).FontSize(18).Bold().FontColor("#147d64");
                c.Item().Text($"{m.OwnerName} · {m.From:yyyy-MM-dd} ~ {m.To:yyyy-MM-dd} · 生成于 {DateTime.Now:yyyy-MM-dd HH:mm}")
                    .FontColor(Colors.Grey.Darken1);
                c.Item().PaddingTop(4).LineHorizontal(1).LineColor("#d9a441");
            });

            page.Content().PaddingVertical(10).Column(c =>
            {
                c.Spacing(12);

                c.Item().Row(r =>
                {
                    r.RelativeItem().Box().Border(1).BorderColor("#e9e5dc").Padding(8)
                        .Column(x => { x.Item().Text("收入").FontColor(Colors.Grey.Darken1); x.Item().Text($"￥{m.Income:0.00}").FontSize(14).Bold().FontColor("#2f9e6e"); });
                    r.ConstantColumn(10);
                    r.RelativeItem().Box().Border(1).BorderColor("#e9e5dc").Padding(8)
                        .Column(x => { x.Item().Text("支出").FontColor(Colors.Grey.Darken1); x.Item().Text($"￥{m.Expense:0.00}").FontSize(14).Bold().FontColor("#c4553d"); });
                    r.ConstantColumn(10);
                    r.RelativeItem().Box().Border(1).BorderColor("#e9e5dc").Padding(8)
                        .Column(x => { x.Item().Text("结余").FontColor(Colors.Grey.Darken1); x.Item().Text($"￥{m.Balance:0.00}").FontSize(14).Bold(); });
                });

                if (m.Deposit is { } d)
                {
                    c.Item().Background("#f7f5f0").Border(1).BorderColor("#e9e5dc").Padding(10).Column(x =>
                    {
                        x.Item().Text("存款利息").Bold();
                        x.Item().Text($"存款本金合计 ￥{d.Principal:0.00} · 当前应计利息 ￥{d.Accrued:0.00} · 本息合计 ￥{d.Total:0.00}");
                    });
                }

                c.Item().Text("月度收支趋势").Bold().FontSize(11);
                c.Item().Image(barPng);

                c.Item().Text("支出分类占比").Bold().FontSize(11);
                c.Item().AlignCenter().Width(240).Image(donutPng);

                if (m.Members.Count > 0)
                {
                    c.Item().Text("成员汇总").Bold().FontSize(11);
                    c.Item().Table(t =>
                    {
                        t.ColumnsDefinition(ct => { ct.RelativeColumn(); ct.RelativeColumn(); ct.RelativeColumn(); ct.RelativeColumn(); ct.RelativeColumn(); ct.RelativeColumn(); });
                        t.Header(h =>
                        {
                            foreach (var (txt, _) in new[] { ("成员", 0), ("收入", 0), ("支出", 0), ("结余", 0), ("存款本金", 0), ("本息合计", 0) })
                                h.Cell().Background("#e3f2ec").Text(txt).Bold();
                        });
                        foreach (var r in m.Members)
                        {
                            t.Cell().Text(r.DisplayName);
                            t.Cell().Text(r.Income.ToString("0.00"));
                            t.Cell().Text(r.Expense.ToString("0.00"));
                            t.Cell().Text(r.Balance.ToString("0.00"));
                            t.Cell().Text(r.DepositPrincipal.ToString("0.00"));
                            t.Cell().Text(r.DepositTotal.ToString("0.00"));
                        }
                    });
                }

                c.Item().Text("明细").Bold().FontSize(11);
                c.Item().Table(t =>
                {
                    t.ColumnsDefinition(ct => { ct.RelativeColumn(0.8f); ct.RelativeColumn(0.6f); ct.RelativeColumn(1f); ct.RelativeColumn(0.8f); ct.RelativeColumn(2f); });
                    t.Header(h =>
                    {
                        foreach (var txt in new[] { "日期", "类型", "分类", "金额", "备注" })
                            h.Cell().Background("#e3f2ec").Text(txt).Bold();
                    });
                    foreach (var row in m.Transactions)
                    {
                        t.Cell().Text(row.Date.ToString("yyyy-MM-dd"));
                        t.Cell().Text(row.Kind == TxKind.Income ? "收入" : "支出");
                        t.Cell().Text(row.CategoryName);
                        t.Cell().Text((row.Kind == TxKind.Income ? "+" : "-") + row.Amount.ToString("0.00"));
                        t.Cell().Text(row.Note);
                    }
                });
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("HomeLedger 家庭记账 · 第 ");
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        })).GeneratePdf(path);
    }
}
