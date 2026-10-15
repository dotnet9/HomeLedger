namespace HomeLedger.Core.Services;

/// <summary>数据库文件备份：复制带时间戳副本到指定目录。</summary>
public static class BackupService
{
    public static string Backup(string dbPath, string targetDir)
    {
        if (!File.Exists(dbPath)) throw new FileNotFoundException(AppText.Get(Language.DatabaseMissing), dbPath);
        Directory.CreateDirectory(targetDir);
        var target = Path.Combine(targetDir, $"ledger-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        File.Copy(dbPath, target, overwrite: true);
        return target;
    }
}
