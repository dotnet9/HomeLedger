using HomeLedger.Core.Data;
using HomeLedger.Core.Services;
using Xunit;

namespace HomeLedger.Core.Tests;

public class DepositServiceTests : IDisposable
{
    private readonly SqliteConnectionFactory _factory;
    private readonly DepositService _deposits;
    private readonly AuthService _auth;
    private readonly long _userId;

    public DepositServiceTests()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hl-test-{Guid.NewGuid():N}.db");
        _factory = new SqliteConnectionFactory(path);
        Db.Initialize(_factory);
        _deposits = new DepositService(_factory);
        _auth = new AuthService(_factory);
        _auth.CreateUser("zhangnan", "张楠", "123456");
        _userId = _auth.ListUsers().Single(u => u.Username == "zhangnan").Id;
    }

    public void Dispose()
    {
        try { _factory.Create().Dispose(); } catch { }
    }

    [Fact]
    public void 存款后可查看链并计息()
    {
        var today = new DateOnly(2026, 8, 21);
        _deposits.AddDeposit(_userId, 10000m, new(2026, 3, 1), 10m, "");
        var chains = _deposits.ListChains(_userId, today);
        var c = Assert.Single(chains);
        Assert.True(c.IsActive);
        // 10000×10%×173/365 = 473.97
        Assert.Equal(473.97m, c.AccruedInterest);
    }

    [Fact]
    public void 结算结转本金_复利()
    {
        var settleDate = new DateOnly(2026, 6, 1);
        _deposits.AddDeposit(_userId, 10000m, new(2026, 3, 1), 10m, "");
        var head = _deposits.ListChains(_userId, settleDate).Single();
        var settlement = _deposits.Settle(_userId, [head.HeadId], settleDate, _userId);

        Assert.Equal(252.05m, settlement.TotalInterest); // 10000×10%×92/365
        var chains = _deposits.ListChains(_userId, settleDate);
        var active = Assert.Single(chains);
        Assert.True(active.IsActive);
        // 结转后新本金 = 原本金 + 应计利息，并从结算日重新起息
        Assert.Equal(10252.05m, active.Principal);
        Assert.Equal(new(2026, 6, 1), active.StartDate);
        Assert.Contains("结转利息 252.05", active.ChainText);
        active = _deposits.ListChains(_userId, new(2026, 7, 1)).Single();
        Assert.Equal(84.26m, active.AccruedInterest); // 10252.05×10%×30/365
        // 结算历史
        Assert.Single(_deposits.ListSettlements(_userId));
    }

    [Fact]
    public void 全额支取结清()
    {
        var today = new DateOnly(2026, 8, 21);
        _deposits.AddDeposit(_userId, 2000m, new(2026, 1, 1), 10m, "");
        var head = _deposits.ListChains(_userId, today).Single();
        _deposits.Withdraw(_userId, head.HeadId, head.Total, today, "");

        var chains = _deposits.ListChains(_userId, today);
        Assert.DoesNotContain(chains, c => c.IsActive);
    }

    [Fact]
    public void 部分支取_剩余续存()
    {
        var today = new DateOnly(2026, 8, 21);
        _deposits.AddDeposit(_userId, 10000m, new(2026, 3, 1), 10m, "");
        var head = _deposits.ListChains(_userId, today).Single();
        var accrued = head.AccruedInterest;

        _deposits.Withdraw(_userId, head.HeadId, 500m, today, "");

        var chains = _deposits.ListChains(_userId, today);
        var active = chains.Single(c => c.IsActive);
        Assert.Equal(10000m + accrued - 500m, active.Principal);
        Assert.Equal(10m, active.AnnualRate);
        Assert.Equal(today, active.StartDate);
    }

    [Fact]
    public void 支取超本息合计抛异常()
    {
        var today = new DateOnly(2026, 8, 21);
        _deposits.AddDeposit(_userId, 1000m, new(2026, 8, 1), 10m, "");
        var head = _deposits.ListChains(_userId, today).Single();
        Assert.Throws<InvalidOperationException>(() => _deposits.Withdraw(_userId, head.HeadId, head.Total + 1, today, ""));
    }

    [Fact]
    public void 已结转的存款不能重复结算()
    {
        var settleDate = new DateOnly(2026, 6, 1);
        _deposits.AddDeposit(_userId, 10000m, new(2026, 3, 1), 10m, "");
        var head = _deposits.ListChains(_userId, settleDate).Single();
        _deposits.Settle(_userId, [head.HeadId], settleDate, _userId);
        Assert.Throws<InvalidOperationException>(() => _deposits.Settle(_userId, [head.HeadId], settleDate, _userId));
    }
}
