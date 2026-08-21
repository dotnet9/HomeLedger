using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using HomeLedger.Core.Export;
using HomeLedger.Core.Services;

namespace HomeLedger.Avalonia.Views.Pages;

public partial class MemberReportPage : UserControl
{
    private static readonly string[] Palette =
        ["#147d64", "#d9a441", "#2f9e6e", "#c4553d", "#b9bdb4", "#6a9fc9", "#c9846a", "#8a7fc9"];

    public MemberReportPage()
    {
        InitializeComponent();
        var years = Enumerable.Range(DateTime.Today.Year - 5, 7).OrderByDescending(y => y).ToList();
        YearBox.ItemsSource = years;
        YearBox.SelectedItem = DateTime.Today.Year;
    }

    private int SelectedYear => YearBox.SelectedItem as int? ?? DateTime.Today.Year;

    private void OnYearChanged(object? sender, SelectionChangedEventArgs e) => Refresh();

    private (DateOnly From, DateOnly To) Range() => (new(SelectedYear, 1, 1), new(SelectedYear, 12, 31));

    private void Refresh()
    {
        var (from, to) = Range();
        var monthly = AppServices.Ledger.MonthlySummary(Session.Current!.Id, from, to);
        BarChart.Items = monthly.Select(m => new Controls.BarItem(m.Month[5..] + "月", (double)m.Income, (double)m.Expense)).ToList();

        var categories = AppServices.Ledger.CategorySummary(Session.Current!.Id, from, to, Core.Models.TxKind.Expense);
        Donut.Items = categories.Select((c, i) => new Controls.DonutItem(c.Category, (double)c.Amount, Palette[i % Palette.Length])).ToList();
        CategoryList.ItemsSource = categories.Select((c, i) => new
        {
            Color = global::Avalonia.Media.Brush.Parse(Palette[i % Palette.Length]),
            c.Category,
            Value = c.Amount.ToString("0.00"),
            Label = c.Category,
        }).ToList();

        MonthlyList.ItemsSource = monthly.Select(m => new
        {
            Month = m.Month,
            Income = "+" + m.Income.ToString("0.00"),
            Expense = "-" + m.Expense.ToString("0.00"),
            Balance = m.Income - m.Expense >= 0 ? "+" + (m.Income - m.Expense).ToString("0.00") : (m.Income - m.Expense).ToString("0.00"),
        }).ToList();

        var (principal, accrued) = AppServices.Deposits.Totals(Session.Current!.Id, DateOnly.FromDateTime(DateTime.Today));
        DepositList.ItemsSource = new (string Label, string Value)[]
        {
            ("存款本金合计", principal.ToString("0.00")),
            ("当前应计利息（未结算）", "+" + accrued.ToString("0.00")),
            ("本息合计", (principal + accrued).ToString("0.00")),
        }.Select(x => new { x.Label, x.Value });
    }

    private async Task<string?> PickFile(string name, params string[] extensions)
    {
        var storage = (VisualRoot as Window)!.StorageProvider;
        var options = new FilePickerSaveOptions
        {
            SuggestedFileName = name,
            FileTypeChoices = [new FilePickerFileType(extensions[0].TrimStart('.')) { Patterns = ["*." + extensions[0].TrimStart('.')] }],
        };
        var file = await storage.SaveFilePickerAsync(options);
        return file?.TryGetLocalPath();
    }

    private async void OnExportCsv(object? sender, RoutedEventArgs e)
    {
        if (await PickFile($"HomeLedger-{SelectedYear}", "csv") is { } path)
            Export(path, ".csv", (m, p) => CsvExporter.Export(m, p));
    }

    private async void OnExportPng(object? sender, RoutedEventArgs e)
    {
        if (await PickFile($"HomeLedger-{SelectedYear}", "png") is { } path)
            Export(path, ".png", (m, p) =>
            {
                var barPng = ChartImageRenderer.MonthlyBars(m.Monthly.Select(r => (r.Month, r.Income, r.Expense)).ToList(), 1100, 420);
                var donutPng = ChartImageRenderer.Donut(m.ExpenseByCategory.Select(c => (c.Category, c.Amount)).ToList(), 460);
                using var bmp = new SkiaSharp.SKBitmap(1100, 920);
                using var canvas = new SkiaSharp.SKCanvas(bmp);
                canvas.Clear(SkiaSharp.SKColors.White);
                canvas.DrawBitmap(SkiaSharp.SKBitmap.Decode(barPng), 0, 0);
                canvas.DrawBitmap(SkiaSharp.SKBitmap.Decode(donutPng), 320, 440);
                using var image = SkiaSharp.SKImage.FromBitmap(bmp);
                using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(p, data.ToArray());
            });
    }

    private async void OnExportPdf(object? sender, RoutedEventArgs e)
    {
        if (await PickFile($"HomeLedger-{SelectedYear}", "pdf") is { } path)
            Export(path, ".pdf", PdfExporter.Export);
    }

    private async void OnExportDocx(object? sender, RoutedEventArgs e)
    {
        if (await PickFile($"HomeLedger-{SelectedYear}", "docx") is { } path)
            Export(path, ".docx", DocxExporter.Export);
    }

    private void Export(string path, string ext, Action<ReportModel, string> writer)
    {
        var (from, to) = Range();
        var model = AppServices.Reports.Build(Session.Current!.Id, from, to, Session.Current.DisplayName);
        var final = Path.ChangeExtension(path, ext);
        writer(model, final);
    }
}
