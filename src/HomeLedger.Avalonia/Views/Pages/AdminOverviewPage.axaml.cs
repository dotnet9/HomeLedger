using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using HomeLedger.Avalonia.Views;
using HomeLedger.Core.Export;
using HomeLedger.Core.Services;

namespace HomeLedger.Avalonia.Views.Pages;

public partial class AdminOverviewPage : UserControl
{
    private static readonly string[] Palette =
        ["#147d64", "#d9a441", "#2f9e6e", "#c4553d", "#b9bdb4", "#6a9fc9", "#c9846a", "#8a7fc9"];

    public AdminOverviewPage()
    {
        InitializeComponent();
        var years = Enumerable.Range(DateTime.Today.Year - 5, 7).OrderByDescending(y => y).ToList();
        YearBox.ItemsSource = years;
        YearBox.SelectedItem = DateTime.Today.Year;
    }

    private int SelectedYear => YearBox.SelectedItem as int? ?? DateTime.Today.Year;
    private (DateOnly From, DateOnly To) Range() => (new(SelectedYear, 1, 1), new(SelectedYear, 12, 31));

    private void OnYearChanged(object? sender, SelectionChangedEventArgs e) => Refresh();


    private void Refresh()
    {
        if (YearBox.SelectedItem is null) return;
        var (from, to) = Range();
        var summary = AppServices.Ledger.SummaryAll(from, to);
        IncomeText.Text = summary.Income.ToString("0.00");
        ExpenseText.Text = summary.Expense.ToString("0.00");
        BalanceText.Text = summary.Balance.ToString("0.00");

        var monthly = AppServices.Ledger.MonthlySummary(null, from, to);
        BarChart.Items = monthly.Select(m => new Controls.BarItem(m.Month[5..] + "月", (double)m.Income, (double)m.Expense)).ToList();

        var categories = AppServices.Ledger.CategorySummary(null, from, to, Core.Models.TxKind.Expense);
        Donut.Items = categories.Select((c, i) => new Controls.DonutItem(c.Category, (double)c.Amount, Palette[i % Palette.Length])).ToList();
        CategoryList.ItemsSource = categories.Select((c, i) => new
        {
            Color = global::Avalonia.Media.Brush.Parse(Palette[i % Palette.Length]),
            Label = c.Category,
            Value = c.Amount.ToString("0.00"),
        }).ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var rows = AppServices.Ledger.MemberSummary(from, to).Select(m =>
        {
            var (p, a) = AppServices.Deposits.Totals(m.UserId, today);
            return new
            {
                Name = m.DisplayName,
                Income = "+" + m.Income.ToString("0.00"),
                Expense = "-" + m.Expense.ToString("0.00"),
                Balance = m.Income - m.Expense >= 0 ? "+" + (m.Income - m.Expense).ToString("0.00") : (m.Income - m.Expense).ToString("0.00"),
                Principal = p.ToString("0.00"),
                Accrued = "+" + a.ToString("0.00"),
                Total = (p + a).ToString("0.00"),
            };
        }).ToList();
        MemberList.ItemsSource = rows;

        var (tp, ta) = AppServices.Deposits.Totals(today);
        DepositTotalText.Text = (tp + ta).ToString("0.00");
    }

    private async Task<string?> PickFile(string ext)
    {
        var storage = TopLevelHost.TryGetStorageProvider(this);
        if (storage is null)
        {
            ShowStatus("无法打开文件保存窗口，请稍后重试", "#c4553d");
            return null;
        }

        FilePickerFileType fileType = new(ext.ToUpperInvariant()) { Patterns = ["*." + ext.TrimStart('.')] };
        IStorageFile? file;
        try
        {
            file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = $"HomeLedger-全家-{SelectedYear}",
                FileTypeChoices = [fileType],
            });
        }
        catch (Exception ex)
        {
            ShowStatus("打开保存窗口失败：" + ex.Message, "#c4553d");
            return null;
        }
        if (file is null) return null;

        var path = file.TryGetLocalPath();
        if (path is null)
            ShowStatus("请选择本机磁盘路径后再导出", "#c4553d");
        return path;
    }

    private async Task ExportAsync(string ext, Action<ReportModel, string> writer)
    {
        var (from, to) = Range();
        var model = AppServices.Reports.Build(null, from, to, "全家");
        if (!HasReportData(model))
        {
            ShowStatus($"{SelectedYear} 年没有可导出的财务数据", "#a97822");
            return;
        }

        var path = await PickFile(ext);
        if (path is null) return;

        try
        {
            var final = Path.ChangeExtension(path, ext);
            writer(model, final);
            ShowStatus("已导出：" + final, "#2f9e6e");
        }
        catch (Exception ex)
        {
            ShowStatus("导出失败：" + ex.Message, "#c4553d");
        }
    }

    private async void OnExportCsv(object? sender, RoutedEventArgs e)
    {
        await ExportAsync("csv", CsvExporter.Export);
    }

    private async void OnExportPdf(object? sender, RoutedEventArgs e)
    {
        await ExportAsync("pdf", PdfExporter.Export);
    }

    private async void OnExportDocx(object? sender, RoutedEventArgs e)
    {
        await ExportAsync("docx", DocxExporter.Export);
    }

    private static bool HasReportData(ReportModel model)
    {
        return model.Transactions.Count > 0
               || model.Monthly.Count > 0
               || model.ExpenseByCategory.Count > 0
               || model.IncomeByCategory.Count > 0
               || model.Deposit is { Principal: not 0 } or { Accrued: not 0 }
               || model.Members.Any(m => m.Income != 0 || m.Expense != 0 || m.DepositTotal != 0);
    }

    private void ShowStatus(string message, string color)
    {
        StatusText.Text = message;
        StatusText.Foreground = Brush.Parse(color);
        StatusText.IsVisible = true;
    }
}
