using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;

namespace HomeLedger.Core.Export;

/// <summary>报表导出（Word .docx）：图表 PNG 嵌入 + 表格。</summary>
public static class DocxExporter
{
    public static void Export(ReportModel m, string path)
    {
        var barPng = ChartImageRenderer.MonthlyBars(m.Monthly.Select(r => (r.Month, r.Income, r.Expense)).ToList());
        var donutPng = ChartImageRenderer.Donut(m.ExpenseByCategory.Select(c => (c.Category, c.Amount)).ToList());

        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new Document(new Body(new Paragraph()));
        var body = main.Document.Body!;

        void H(string text, int size = 16)
        {
            body.Append(new Paragraph(new Run(new RunProperties(new Bold(), new FontSize() { Val = (size * 2).ToString() }, new Color { Val = "147D64" }),
                new Text(text))));
        }

        void T(string text)
        {
            body.Append(new Paragraph(new Run(new RunProperties(new FontSize() { Val = "40" }, new Color { Val = "5A6058" }), new Text(text))));
        }

        void Table(string[] headers, IEnumerable<string[]> rows)
        {
            var tbl = new Table();
            tbl.Append(new TableProperties(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "E9E5DC" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "E9E5DC" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "E9E5DC" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "E9E5DC" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "E9E5DC" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "E9E5DC" })));
            var tr = new TableRow();
            foreach (var h in headers)
                tr.Append(new TableCell(new Paragraph(new Run(new RunProperties(new Bold(), new FontSize() { Val = "36" }), new Text(h)))));
            tbl.Append(tr);
            foreach (var row in rows)
            {
                tr = new TableRow();
                foreach (var cell in row)
                    tr.Append(new TableCell(new Paragraph(new Run(new RunProperties(new FontSize() { Val = "36" }), new Text(cell)))));
                tbl.Append(tr);
            }
            body.Append(tbl);
        }

        void Image(byte[] png, int widthEmu)
        {
            var imagePart = main.AddImagePart(ImagePartType.Png);
            using (var ms = new MemoryStream(png)) imagePart.FeedData(ms);
            var rId = main.GetIdOfPart(imagePart);
            var cx = widthEmu; var cy = (int)(widthEmu * 0.42);
            var pic = new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties { Id = 1U, Name = "chart" },
                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()),
                new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                    new DocumentFormat.OpenXml.Drawing.Blip { Embed = rId },
                    new DocumentFormat.OpenXml.Drawing.Stretch(new DocumentFormat.OpenXml.Drawing.FillRectangle())),
                new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                    new DocumentFormat.OpenXml.Drawing.Transform2D(
                        new DocumentFormat.OpenXml.Drawing.Offset { X = 0, Y = 0 },
                        new DocumentFormat.OpenXml.Drawing.Extents { Cx = cx, Cy = cy })));
            var draw = new DocumentFormat.OpenXml.Wordprocessing.Drawing(
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                    new DocumentFormat.OpenXml.Drawing.Graphic(
                        new DocumentFormat.OpenXml.Drawing.GraphicData(pic) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
                { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });
            body.Append(new Paragraph(new Run(draw)));
        }

        H(m.Title);
        T($"{m.OwnerName} · {m.From:yyyy-MM-dd} ~ {m.To:yyyy-MM-dd} · 生成于 {DateTime.Now:yyyy-MM-dd HH:mm}");

        H("收支汇总", 13);
        Table(["项", "金额"], [["收入", m.Income.ToString("0.00")], ["支出", m.Expense.ToString("0.00")], ["结余", m.Balance.ToString("0.00")]]);

        if (m.Deposit is { } d)
        {
            H("存款利息", 13);
            Table(["项", "金额"], [["存款本金合计", d.Principal.ToString("0.00")], ["当前应计利息", d.Accrued.ToString("0.00")], ["本息合计", d.Total.ToString("0.00")]]);
        }

        H("月度收支趋势", 13);
        Image(barPng, 5400000);

        H("支出分类占比", 13);
        Image(donutPng, 2400000);

        if (m.Members.Count > 0)
        {
            H("成员汇总", 13);
            Table(["成员", "收入", "支出", "结余", "存款本金", "本息合计"],
                m.Members.Select(r => new[] { r.DisplayName, r.Income.ToString("0.00"), r.Expense.ToString("0.00"), r.Balance.ToString("0.00"), r.DepositPrincipal.ToString("0.00"), r.DepositTotal.ToString("0.00") }));
        }

        H("明细", 13);
        Table(["日期", "类型", "分类", "金额", "备注"],
            m.Transactions.Select(t => new[] { t.Date.ToString("yyyy-MM-dd"), t.Kind == TxKind.Income ? "收入" : "支出", t.CategoryName, t.Amount.ToString("0.00"), t.Note }));

        body.Append(new SectionProperties(new PageMargin { Top = 720, Bottom = 720, Left = 720, Right = 720 }));
    }
}
