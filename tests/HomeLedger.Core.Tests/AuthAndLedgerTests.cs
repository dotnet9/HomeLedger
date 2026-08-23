using HomeLedger.Core.Data;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using Xunit;

namespace HomeLedger.Core.Tests;

public class AuthAndLedgerTests : IDisposable
{
    private readonly SqliteConnectionFactory _factory;
    private readonly AuthService _auth;
    private readonly LedgerService _ledger;

    public AuthAndLedgerTests()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hl-test-{Guid.NewGuid():N}.db");
        _factory = new SqliteConnectionFactory(path);
        Db.Initialize(_factory);
        _auth = new AuthService(_factory);
        _ledger = new LedgerService(_factory);
        _auth.CreateUser("baba", "爸爸", "123456");
        _auth.CreateUser("mama", "妈妈", "123456");
    }

    public void Dispose()
    {
        try { _factory.Create().Dispose(); } catch { }
    }

    [Fact]
    public void 初始化后存在默认管理员()
    {
        var admin = _auth.Login("admin", "admin123");
        Assert.NotNull(admin);
        Assert.Equal(Role.Admin, admin!.Role);
        Assert.True(admin.MustChangePassword);
    }

    [Fact]
    public void 错误密码登录失败()
    {
        Assert.Null(_auth.Login("admin", "wrong"));
        Assert.Null(_auth.Login("nobody", "123456"));
    }

    [Fact]
    public void 修改密码后旧密码失效()
    {
        var user = _auth.Login("baba", "123456")!;
        _auth.ChangePassword(user.Id, "newpass9");
        Assert.Null(_auth.Login("baba", "123456"));
        var again = _auth.Login("baba", "newpass9");
        Assert.NotNull(again);
        Assert.False(again!.MustChangePassword);
    }

    [Fact]
    public void 停用账号不能登录_数据保留()
    {
        var user = _auth.Login("baba", "123456")!;
        _ledger.AddTransaction(user.Id, TxKind.Expense, 100m, new(2026, 1, 1), null, "test");
        _auth.SetActive(user.Id, false);
        Assert.Null(_auth.Login("baba", "123456"));
        Assert.Single(_ledger.ListTransactions(new TransactionFilter { UserId = user.Id }));
    }

    [Fact]
    public void 成员只能查到自己的记录_数据隔离()
    {
        var baba = _auth.Login("baba", "123456")!;
        var mama = _auth.Login("mama", "123456")!;
        _ledger.AddTransaction(baba.Id, TxKind.Income, 1000m, new(2026, 1, 1), null, "");
        _ledger.AddTransaction(mama.Id, TxKind.Expense, 500m, new(2026, 1, 2), null, "");

        Assert.Single(_ledger.ListTransactions(new TransactionFilter { UserId = baba.Id }));
        Assert.Single(_ledger.ListTransactions(new TransactionFilter { UserId = mama.Id }));
        Assert.Equal(2, _ledger.ListTransactions(new TransactionFilter()).Count);
    }

    [Fact]
    public void 汇总与按月统计正确()
    {
        var baba = _auth.Login("baba", "123456")!;
        _ledger.AddTransaction(baba.Id, TxKind.Income, 1000m, new(2026, 1, 5), null, "");
        _ledger.AddTransaction(baba.Id, TxKind.Expense, 300m, new(2026, 1, 6), null, "");
        _ledger.AddTransaction(baba.Id, TxKind.Expense, 200m, new(2026, 2, 1), null, "");

        var s = _ledger.Summary(baba.Id, new(2026, 1, 1), new(2026, 12, 31));
        Assert.Equal(1000m, s.Income);
        Assert.Equal(500m, s.Expense);

        var monthly = _ledger.MonthlySummary(baba.Id, new(2026, 1, 1), new(2026, 12, 31));
        Assert.Equal(2, monthly.Count);
        Assert.Equal(700m, monthly[0].Income - monthly[0].Expense);
    }

    [Fact]
    public void 删除为逻辑删除()
    {
        var baba = _auth.Login("baba", "123456")!;
        _ledger.AddTransaction(baba.Id, TxKind.Expense, 100m, new(2026, 1, 1), null, "");
        var item = _ledger.ListTransactions(new TransactionFilter { UserId = baba.Id }).Single();
        _ledger.DeleteTransaction(baba.Id, item.Id);
        Assert.Empty(_ledger.ListTransactions(new TransactionFilter { UserId = baba.Id }));
    }

    [Fact]
    public void 记账金额必须大于零()
    {
        var baba = _auth.Login("baba", "123456")!;

        Assert.Throws<InvalidOperationException>(() =>
            _ledger.AddTransaction(baba.Id, TxKind.Expense, 0m, new(2026, 1, 1), null, ""));
        Assert.Throws<InvalidOperationException>(() =>
            _ledger.AddTransaction(baba.Id, TxKind.Income, -1m, new(2026, 1, 1), null, ""));
    }

    [Fact]
    public void 不能使用其他成员的自定义分类()
    {
        var baba = _auth.Login("baba", "123456")!;
        var mama = _auth.Login("mama", "123456")!;
        var mamaCategoryId = _ledger.AddCategory(mama.Id, "妈妈专属", TxKind.Expense);

        Assert.Throws<InvalidOperationException>(() =>
            _ledger.AddTransaction(baba.Id, TxKind.Expense, 10m, new(2026, 1, 1), mamaCategoryId, ""));
        Assert.Empty(_ledger.ListTransactions(new TransactionFilter { UserId = baba.Id }));
    }

    [Fact]
    public void 分类筛选只返回指定分类记录()
    {
        var baba = _auth.Login("baba", "123456")!;
        var foodId = _ledger.AddCategory(baba.Id, "外食", TxKind.Expense);
        var trafficId = _ledger.AddCategory(baba.Id, "打车", TxKind.Expense);
        _ledger.AddTransaction(baba.Id, TxKind.Expense, 80m, new(2026, 1, 1), foodId, "午饭");
        _ledger.AddTransaction(baba.Id, TxKind.Expense, 30m, new(2026, 1, 2), trafficId, "通勤");

        var rows = _ledger.ListTransactions(new TransactionFilter { UserId = baba.Id, CategoryId = foodId });

        var row = Assert.Single(rows);
        Assert.Equal("外食", row.CategoryName);
        Assert.Equal(80m, row.Amount);
    }

    [Fact]
    public void 管理员分类列表包含成员自定义分类()
    {
        var baba = _auth.Login("baba", "123456")!;
        _ledger.AddCategory(baba.Id, "爸爸自定义支出", TxKind.Expense);

        var categories = _ledger.ListAllCategories(TxKind.Expense);

        Assert.Contains(categories, c => c.Name == "爸爸自定义支出" && c.UserId == baba.Id);
    }

    [Fact]
    public void 报表包含存款利息与成员汇总()
    {
        var baba = _auth.Login("baba", "123456")!;
        new DepositService(_factory).AddDeposit(baba.Id, 5000m, new(2026, 1, 1), 10m, "");
        _ledger.AddTransaction(baba.Id, TxKind.Income, 1000m, new(2026, 3, 1), null, "");

        var reports = new ReportBuilder(_ledger, new DepositService(_factory));
        var model = reports.Build(null, new(2026, 1, 1), new(2026, 12, 31), "全家");
        Assert.Equal(1000m, model.Income);
        Assert.NotNull(model.Deposit);
        Assert.Equal(5000m, model.Deposit!.Principal);
        Assert.Contains(model.Members, m => m.DisplayName == "爸爸" && m.Income == 1000m);
    }

    [Fact]
    public void CSV导出写入文件()
    {
        var baba = _auth.Login("baba", "123456")!;
        _ledger.AddTransaction(baba.Id, TxKind.Income, 1000m, new(2026, 3, 1), null, "工资");
        var model = new ReportBuilder(_ledger, new DepositService(_factory))
            .Build(baba.Id, new(2026, 1, 1), new(2026, 12, 31), "爸爸");
        var path = Path.Combine(Path.GetTempPath(), $"hl-csv-{Guid.NewGuid():N}.csv");
        Export.CsvExporter.Export(model, path);
        Assert.True(File.Exists(path));
        Assert.Contains("工资", File.ReadAllText(path));
    }

    [Fact]
    public void 空报表导出不会异常()
    {
        var baba = _auth.Login("baba", "123456")!;
        var model = new ReportBuilder(_ledger, new DepositService(_factory))
            .Build(baba.Id, new(2035, 1, 1), new(2035, 12, 31), "爸爸");

        Assert.Empty(model.Transactions);
        Assert.Empty(model.Monthly);

        var csv = Path.Combine(Path.GetTempPath(), $"hl-empty-{Guid.NewGuid():N}.csv");
        var pdf = Path.Combine(Path.GetTempPath(), $"hl-empty-{Guid.NewGuid():N}.pdf");
        var docx = Path.Combine(Path.GetTempPath(), $"hl-empty-{Guid.NewGuid():N}.docx");

        Export.CsvExporter.Export(model, csv);
        Export.PdfExporter.Export(model, pdf);
        Export.DocxExporter.Export(model, docx);

        Assert.Contains("明细", File.ReadAllText(csv));
        Assert.True(new FileInfo(pdf).Length > 0);
        Assert.True(new FileInfo(docx).Length > 0);
    }
}
