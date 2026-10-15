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
    public static string DataDirectory { get; } = Environment.GetEnvironmentVariable("HOMELEDGER_DATA_DIRECTORY") ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HomeLedger");
    public static readonly string DbPath = ResolveDbPath();

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
        Reports = new ReportBuilder(Ledger, Deposits);
    }

    private static string ResolveDbPath()
    {
        var directory = DataDirectory;
        var dbPath = Path.Combine(directory, "ledger.db");

        // 旧版把库放在 exe 旁；首次升级时迁移已有数据到应用数据目录。
        var legacyPath = Path.Combine(AppContext.BaseDirectory, "ledger.db");
        if (Environment.GetEnvironmentVariable("HOMELEDGER_DATA_DIRECTORY") is null && !File.Exists(dbPath) && File.Exists(legacyPath))
        {
            Directory.CreateDirectory(directory);
            File.Copy(legacyPath, dbPath);
        }

        return dbPath;
    }
}
