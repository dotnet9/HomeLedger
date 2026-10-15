using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using HomeLedger.Avalonia.Views;
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

    public void Refresh()
    {
        var (from, to) = Range();
        var monthly = AppServices.Ledger.MonthlySummary(Session.Current!.Id, from, to);
        BarChart.Items = monthly.Select(m => new Controls.BarItem(DateTime.ParseExact(m.Month, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture).ToString("MMM", AppText.Culture), (double)m.Income, (double)m.Expense)).ToList();

        var categories = AppServices.Ledger.CategorySummary(Session.Current!.Id, from, to, Core.Models.TxKind.Expense);
        Donut.Items = categories.Select((c, i) => new Controls.DonutItem(c.Category, (double)c.Amount, Palette[i % Palette.Length])).ToList();
        CategoryList.ItemsSource = categories.Select((c, i) => new
        {
            Color = global::Avalonia.Media.Brush.Parse(Palette[i % Palette.Length]),
            c.Category,
            Value = c.Amount.ToString("0.00"),
            Label = AppText.Category(c.Category),
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
            (AppText.Get(Language.TotalDepositPrincipal), principal.ToString("0.00")),
            (AppText.Get(Language.UnsettledInterest), "+" + accrued.ToString("0.00")),
            (AppText.Get(Language.PrincipalAndInterest), (principal + accrued).ToString("0.00")),
        }.Select(x => new { x.Label, x.Value });
    }

    private async Task<string?> PickFile(string name, params string[] extensions)
    {
        var storage = TopLevelHost.TryGetStorageProvider(this);
        if (storage is null)
        {
            ShowStatus(AppText.Get(Language.SavePickerUnavailable), "FinanceExpense");
            return null;
        }

        var extension = extensions[0].TrimStart('.');
        var options = new FilePickerSaveOptions
        {
            SuggestedFileName = name,
            FileTypeChoices = [new FilePickerFileType(extension.ToUpperInvariant()) { Patterns = ["*." + extension] }],
        };
        IStorageFile? file;
        try
        {
            file = await storage.SaveFilePickerAsync(options);
        }
        catch (Exception ex)
        {
            ShowStatus(AppText.Get(Language.SavePickerFailed) + ex.Message, "FinanceExpense");
            return null;
        }
        if (file is null) return null;

        var path = file.TryGetLocalPath();
        if (path is null)
            ShowStatus(AppText.Get(Language.LocalExportRequired), "FinanceExpense");
        return path;
    }

    private async void OnExportCsv(object? sender, RoutedEventArgs e)
    {
        await ExportAsync("csv", CsvExporter.Export);
    }

    private async void OnExportPng(object? sender, RoutedEventArgs e)
    {
        var model = BuildReport();
        if (!HasChartData(model))
        {
            ShowStatus(AppText.Format(Language.NoChartData, SelectedYear), "#a97822");
            return;
        }

        await ExportAsync("png", ExportChartPng, model);
    }

    private async void OnExportPdf(object? sender, RoutedEventArgs e)
    {
        await ExportAsync("pdf", PdfExporter.Export);
    }

    private async void OnExportDocx(object? sender, RoutedEventArgs e)
    {
        await ExportAsync("docx", DocxExporter.Export);
    }

    private ReportModel BuildReport()
    {
        var (from, to) = Range();
        return AppServices.Reports.Build(Session.Current!.Id, from, to, Session.Current.DisplayName);
    }

    private async Task ExportAsync(string ext, Action<ReportModel, string> writer, ReportModel? model = null)
    {
        model ??= BuildReport();
        if (!HasReportData(model))
        {
            ShowStatus(AppText.Format(Language.NoFinancialData, SelectedYear), "#a97822");
            return;
        }

        var path = await PickFile($"HomeLedger-{SelectedYear}", ext);
        if (path is null) return;

        try
        {
            var final = Path.ChangeExtension(path, ext);
            writer(model, final);
            ShowStatus(AppText.Get(Language.ExportSucceeded) + final, "FinanceIncome");
        }
        catch (Exception ex)
        {
            ShowStatus(AppText.Get(Language.ExportFailed) + ex.Message, "FinanceExpense");
        }
    }

    private static bool HasReportData(ReportModel model)
    {
        return model.Transactions.Count > 0
               || model.Monthly.Count > 0
               || model.ExpenseByCategory.Count > 0
               || model.IncomeByCategory.Count > 0
               || model.Deposit is { Principal: not 0 } or { Accrued: not 0 };
    }

    private static bool HasChartData(ReportModel model)
    {
        return model.Monthly.Count > 0 || model.ExpenseByCategory.Count > 0;
    }

    private static void ExportChartPng(ReportModel model, string path)
    {
        var barPng = ChartImageRenderer.MonthlyBars(model.Monthly.Select(r => (r.Month, r.Income, r.Expense)).ToList(), 1100, 420);
        var donutPng = ChartImageRenderer.Donut(model.ExpenseByCategory.Select(c => (c.Category, c.Amount)).ToList(), 460);
        using var bmp = new SkiaSharp.SKBitmap(1100, 920);
        using var canvas = new SkiaSharp.SKCanvas(bmp);
        canvas.Clear(SkiaSharp.SKColors.White);
        using var barBitmap = SkiaSharp.SKBitmap.Decode(barPng);
        using var donutBitmap = SkiaSharp.SKBitmap.Decode(donutPng);
        var sampling = new SkiaSharp.SKSamplingOptions(SkiaSharp.SKFilterMode.Linear, SkiaSharp.SKMipmapMode.None);
        canvas.DrawBitmap(barBitmap, 0, 0, sampling);
        canvas.DrawBitmap(donutBitmap, 320, 440, sampling);
        using var image = SkiaSharp.SKImage.FromBitmap(bmp);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }

    private void ShowStatus(string message, string color)
    {
        StatusText.Text = message;
        StatusText.Foreground = HomeLedger.Avalonia.Services.ThemeBrush.Get(color);
        StatusText.IsVisible = true;
    }
}
