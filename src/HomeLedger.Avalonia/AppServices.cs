using HomeLedger.Core.Data;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;

namespace HomeLedger.Avalonia;

/// <summary>当前登录会话。</summary>
public static class Session
{
    public static User? Current { get; set; }
}

/// <summary>简易服务定位器（单机自用，无并发注册需求）。</summary>
public static class AppServices
{
    public static readonly string DbPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HomeLedger", "ledger.db");

    public static IDbConnectionFactory Db { get; }
    public static AuthService Auth { get; }
    public static LedgerService Ledger { get; }
    public static DepositService Deposits { get; }
    public static ReportBuilder Reports { get; }

    static AppServices()
    {
        Db = new SqliteConnectionFactory(DbPath);
        Core.Data.Db.Initialize(Db);
        Auth = new AuthService(Db);
        Ledger = new LedgerService(Db);
        Deposits = new DepositService(Db);
        Reports = new ReportBuilder(Ledger, Deposits, Auth);
    }
}
