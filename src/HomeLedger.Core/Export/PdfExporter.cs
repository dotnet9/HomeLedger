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
                c.Item().Text(AppText.Format(Language.ReportGenerated, m.OwnerName, m.From, m.To, DateTime.Now))
                    .FontColor(Colors.Grey.Darken1);
                c.Item().PaddingTop(4).LineHorizontal(1).LineColor("#d9a441");
            });

            page.Content().PaddingVertical(10).Column(c =>
            {
                c.Spacing(12);

                c.Item().Row(r =>
                {
                    r.RelativeItem().Shrink().Border(1).BorderColor("#e9e5dc").Padding(8)
                        .Column(x => { x.Item().Text(AppText.Get(Language.Income)).FontColor(Colors.Grey.Darken1); x.Item().Text($"￥{m.Income:0.00}").FontSize(14).Bold().FontColor("#2f9e6e"); });
                    r.ConstantItem(10);
                    r.RelativeItem().Shrink().Border(1).BorderColor("#e9e5dc").Padding(8)
                        .Column(x => { x.Item().Text(AppText.Get(Language.Expense)).FontColor(Colors.Grey.Darken1); x.Item().Text($"￥{m.Expense:0.00}").FontSize(14).Bold().FontColor("#c4553d"); });
                    r.ConstantItem(10);
                    r.RelativeItem().Shrink().Border(1).BorderColor("#e9e5dc").Padding(8)
                        .Column(x => { x.Item().Text(AppText.Get(Language.Balance)).FontColor(Colors.Grey.Darken1); x.Item().Text($"￥{m.Balance:0.00}").FontSize(14).Bold(); });
                });

                if (m.Deposit is { } d)
                {
                    c.Item().Background("#f7f5f0").Border(1).BorderColor("#e9e5dc").Padding(10).Column(x =>
                    {
                        x.Item().Text(AppText.Get(Language.DepositInterest)).Bold();
                        x.Item().Text(AppText.Format(Language.PdfDepositSummary, d.Principal, d.Accrued, d.Total));
                    });
                }

                c.Item().Text(AppText.Get(Language.MonthlyTrend)).Bold().FontSize(11);
                c.Item().Image(barPng);

                c.Item().Text(AppText.Get(Language.ExpenseCategoryShare)).Bold().FontSize(11);
                c.Item().AlignCenter().Width(240).Image(donutPng);

                if (m.Members.Count > 0)
                {
                    c.Item().Text(AppText.Get(Language.MemberSummary)).Bold().FontSize(11);
                    c.Item().Table(t =>
                    {
                        t.ColumnsDefinition(ct => { ct.RelativeColumn(); ct.RelativeColumn(); ct.RelativeColumn(); ct.RelativeColumn(); ct.RelativeColumn(); ct.RelativeColumn(); });
                        t.Header(h =>
                        {
                            foreach (var (txt, _) in new[] { (AppText.Get(Language.Member), 0), (AppText.Get(Language.Income), 0), (AppText.Get(Language.Expense), 0), (AppText.Get(Language.Balance), 0), (AppText.Get(Language.DepositPrincipal), 0), (AppText.Get(Language.PrincipalAndInterest), 0) })
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

                c.Item().Text(AppText.Get(Language.Details)).Bold().FontSize(11);
                c.Item().Table(t =>
                {
                    t.ColumnsDefinition(ct => { ct.RelativeColumn(0.8f); ct.RelativeColumn(0.6f); ct.RelativeColumn(1f); ct.RelativeColumn(0.8f); ct.RelativeColumn(2f); });
                    t.Header(h =>
                    {
                        foreach (var txt in new[] { AppText.Get(Language.Date), AppText.Get(Language.Type), AppText.Get(Language.Category), AppText.Get(Language.Amount), AppText.Get(Language.Note) })
                            h.Cell().Background("#e3f2ec").Text(txt).Bold();
                    });
                    foreach (var row in m.Transactions)
                    {
                        t.Cell().Text(row.Date.ToString("yyyy-MM-dd"));
                        t.Cell().Text(row.Kind == TxKind.Income ? AppText.Get(Language.Income) : AppText.Get(Language.Expense));
                        t.Cell().Text(row.CategoryName);
                        t.Cell().Text((row.Kind == TxKind.Income ? "+" : "-") + row.Amount.ToString("0.00"));
                        t.Cell().Text(row.Note);
                    }
                });
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span(AppText.Get(Language.PdfPagePrefix));
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        })).GeneratePdf(path);
    }
}
