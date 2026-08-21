using HomeLedger.Core.Services;
using Xunit;

namespace HomeLedger.Core.Tests;

public class InterestCalculatorTests
{
    [Fact]
    public void 整年利息_按365天单利()
    {
        // 10000 × 10% × 365/365 = 1000
        Assert.Equal(1000m, InterestCalculator.Accrued(10000m, 10m, new(2025, 1, 1), new(2026, 1, 1)));
    }

    [Fact]
    public void 半年利息_四舍五入到分()
    {
        // 5000 × 8% × 182/365 ≈ 199.45
        Assert.Equal(199.45m, InterestCalculator.Accrued(5000m, 8m, new(2025, 1, 1), new(2025, 7, 2)));
    }

    [Fact]
    public void 起息日当天利息为零()
    {
        Assert.Equal(0m, InterestCalculator.Accrued(10000m, 10m, new(2025, 6, 1), new(2025, 6, 1)));
    }

    [Fact]
    public void 截止日早于起息日_不产生负利息()
    {
        Assert.Equal(0m, InterestCalculator.Accrued(10000m, 10m, new(2025, 6, 1), new(2025, 5, 1)));
    }

    [Fact]
    public void 零本金或零利率不计息()
    {
        Assert.Equal(0m, InterestCalculator.Accrued(0m, 10m, new(2025, 1, 1), new(2026, 1, 1)));
        Assert.Equal(0m, InterestCalculator.Accrued(10000m, 0m, new(2025, 1, 1), new(2026, 1, 1)));
    }
}
