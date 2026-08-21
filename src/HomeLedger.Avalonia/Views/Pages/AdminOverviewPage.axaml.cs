using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
        var storage = (VisualRoot as Window)!.StorageProvider;
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = $"HomeLedger-全家-{SelectedYear}",
            FileTypeChoices = [new FilePickerFileType(ext) { Patterns = ["*." + ext] }],
        });
        return file?.TryGetLocalPath();
    }

    private void Export(string path, string ext, Action<ReportModel, string> writer)
    {
        var (from, to) = Range();
        var model = AppServices.Reports.Build(null, from, to, "全家");
        writer(model, Path.ChangeExtension(path, ext));
    }

    private async void OnExportCsv(object? sender, RoutedEventArgs e)
    {
        if (await PickFile("csv") is { } path) Export(path, "csv", CsvExporter.Export);
    }

    private async void OnExportPdf(object? sender, RoutedEventArgs e)
    {
        if (await PickFile("pdf") is { } path) Export(path, "pdf", PdfExporter.Export);
    }

    private async void OnExportDocx(object? sender, RoutedEventArgs e)
    {
        if (await PickFile("docx") is { } path) Export(path, "docx", DocxExporter.Export);
    }
}
