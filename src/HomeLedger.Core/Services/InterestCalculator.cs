namespace HomeLedger.Core.Services;

/// <summary>
/// 单利按日计息：利息 = 本金 × 年利率(百分数) ÷ 100 × 天数 ÷ 365。
/// 复利通过“结算结转本金”实现，见 DepositService。
/// </summary>
public static class InterestCalculator
{
    public static int Days(DateOnly start, DateOnly until) => Math.Max(0, until.DayNumber - start.DayNumber);

    public static decimal Accrued(decimal principal, decimal annualRatePercent, DateOnly start, DateOnly until)
    {
        if (principal <= 0 || annualRatePercent <= 0) return 0m;
        var interest = principal * (annualRatePercent / 100m) * Days(start, until) / 365m;
        return Math.Round(interest, 2, MidpointRounding.AwayFromZero);
    }
}
