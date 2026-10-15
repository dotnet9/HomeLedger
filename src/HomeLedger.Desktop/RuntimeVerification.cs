using System.Runtime.CompilerServices;
using HomeLedger.Core.Data;
using HomeLedger.Core.I18n;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;

namespace HomeLedger.Desktop;

/// <summary>显式命令行验证原生发布产物；不创建桌面窗口，只使用调用方指定的独立目录。</summary>
internal static class RuntimeVerification
{
    public static void Run(string directory)
    {
        if (RuntimeFeature.IsDynamicCodeSupported)
            throw new InvalidOperationException("Runtime verification requires a NativeAOT executable.");
        var root = Path.GetFullPath(directory);
        Directory.CreateDirectory(root);
        var database = Path.Combine(root, "runtime-" + Guid.NewGuid().ToString("N") + ".db");
        AppText.Initialize("en-US");
        if (AppText.Get(Language.Income) != "Income") throw new InvalidOperationException("Published I18n resources are missing.");
        var factory = new SqliteConnectionFactory(database);
        Db.Initialize(factory);
        var auth = new AuthService(factory);
        auth.CreateUser("runtime", "Runtime verification", "verification-only");
        var member = auth.Login("runtime", "verification-only") ?? throw new InvalidOperationException("Native authentication mapping failed.");
        var date = new DateOnly(2026, 10, 15);
        var ledger = new LedgerService(factory);
        ledger.AddTransaction(member.Id, TxKind.Income, 1234.56m, date, null, "NativeAOT fixture");
        var row = ledger.ListTransactions(new() { UserId = member.Id, From = date, To = date }).Single();
        if (row.Amount != 1234.56m || row.Date != date || ledger.Summary(member.Id, date, date).Income != 1234.56m)
            throw new InvalidOperationException("Native date/money/filter mapping failed.");
        var deposits = new DepositService(factory);
        var head = deposits.AddDeposit(member.Id, 10000m, date.AddDays(-365), 10m, "NativeAOT fixture");
        var settlement = deposits.Settle(member.Id, [head], date, member.Id);
        if (settlement.TotalInterest != 1000m || deposits.ListSettlements(member.Id).Single().DepositCount != 1)
            throw new InvalidOperationException("Native settlement mapping failed.");
        deposits.Withdraw(member.Id, deposits.ListChains(member.Id, date).Single().HeadId, 100m, date, "NativeAOT fixture");
        if (deposits.ListChains(member.Id, date).Count != 2)
            throw new InvalidOperationException("Native partial withdrawal mapping failed.");
        var result = $"NativeAOT runtime verification passed; version={typeof(Program).Assembly.GetName().Version}; database={database}";
        File.WriteAllText(Path.Combine(root, "runtime-verification.txt"), result);
        Console.WriteLine(result);
    }
}
