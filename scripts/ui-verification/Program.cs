using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CodeWF.Avalonia.Controls.Themes;
using CodeWF.Toolkit.Core.UpdateChecking;
using CodeWF.Toolkit.Lang;
using HomeLedger.Avalonia;
using HomeLedger.Avalonia.Services;
using HomeLedger.Avalonia.ViewModels;
using HomeLedger.Avalonia.Views;
using HomeLedger.Core.I18n;
using HomeLedger.Core.Models;
using HomeLedger.Core.Services;
using HomeLedger.Desktop;

var output = Path.GetFullPath(args.FirstOrDefault() ?? Path.Combine(Path.GetTempPath(), "homeledger-ui"));
Directory.CreateDirectory(output);
Environment.SetEnvironmentVariable("HOMELEDGER_DATA_DIRECTORY", Path.Combine(output, "isolated-data-" + Guid.NewGuid().ToString("N")));
AppBuilder.Configure<App>().UseSkia().With(new FontManagerOptions
{
    FontFallbacks = [new FontFallback { FontFamily = new FontFamily("Microsoft YaHei UI") }]
}).UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
var assertions = new List<string>();
var appearance = AppearanceService.Current;
Check(appearance.Modes.Count == 3 && appearance.Palettes.Count == 8, "Three modes and eight shared palettes");
Check(AppServices.DbPath.StartsWith(output, StringComparison.OrdinalIgnoreCase), "All database work is isolated");
AppServices.Auth.CreateUser("member", "Alex", "test1234");
AppServices.Auth.CreateUser("member2", "Sam", "test1234");
var member = AppServices.Auth.Login("member", "test1234")!;
var other = AppServices.Auth.Login("member2", "test1234")!;
var admin = AppServices.Auth.Login("admin", "admin123")!;
var today = DateOnly.FromDateTime(DateTime.Today);
for (var i = 0; i < 12; i++)
{
    var date = new DateOnly(today.Year, i + 1, 3);
    AppServices.Ledger.AddTransaction(member.Id, TxKind.Income, 3200m, date, null, "Income sample");
    AppServices.Ledger.AddTransaction(member.Id, TxKind.Expense, 2450m + i, date, 1, "Household sample");
    AppServices.Ledger.AddTransaction(other.Id, TxKind.Income, 5000m, date, null, "Other member sample");
}
AppServices.Deposits.AddDeposit(member.Id, 10000m, today.AddDays(-365), 10m, "Savings");
AppServices.Deposits.AddDeposit(member.Id, 4000m, today.AddDays(-180), 5m, "Travel fund");
AppServices.Deposits.AddDeposit(other.Id, 18000m, today.AddDays(-120), 4m, "Family reserve");
Session.Current = member;
var login = new LoginWindow();
login.Show(); Capture(login, "login-zh-CN.png"); login.Close();
var window = new MainWindow(); window.Show();
var model = (MainWindowViewModel)window.DataContext!;
var ledger = (MemberLedgerPageViewModel)model.CurrentPage!.DataContext!;
ledger.QuickAmountText = "123.45";
ledger.QuickNote = "Headless quick entry";
ledger.SaveQuickCommand.Execute(null); Pump();
Check(ledger.Transactions.Any(row => row.Note == "Headless quick entry"), "Real quick-entry command saves a transaction");
ledger.Keyword = "Headless quick entry"; ledger.FilterCommand.Execute();
Check(ledger.Transactions.Count == 1, "Note filter reaches the real ledger service");
ledger.Transactions[0].DeleteCommand.Execute(null);
Check(ledger.Transactions.Count == 0, "Delete action uses the existing soft-delete flow");
ledger.Keyword = null; ledger.FilterCommand.Execute();
Check(ledger.Transactions.All(row => row.Item.UserId == member.Id), "Member rows keep user isolation");
ledger.FromDate = new DateTimeOffset(new DateTime(today.Year, 1, 1));
ledger.ToDate = new DateTimeOffset(new DateTime(today.Year, 12, 31));
ledger.FilterCommand.Execute();
var firstPageIds = ledger.Transactions.Select(row => row.Item.Id).ToArray();
Check(firstPageIds.Length == 10 && ledger.NextPageCommand.CanExecute(), "Ledger paginates filtered records by ten");
ledger.NextPageCommand.Execute();
Check(ledger.Transactions.All(row => !firstPageIds.Contains(row.Item.Id)) && ledger.PreviousPageCommand.CanExecute(), "Next page contains different persisted records");
ledger.PreviousPageCommand.Execute();
Check(ledger.Transactions.Select(row => row.Item.Id).SequenceEqual(firstPageIds), "Previous page restores the first records");
ledger.QuickAmountText = "42.70"; ledger.QuickNote = "Keep draft";
appearance.SelectedLanguage = appearance.Languages[1]; Pump();
Check(ledger.QuickAmountText == "42.70" && ledger.QuickNote == "Keep draft", "Language switching preserves the quick-entry draft");
appearance.SelectedLanguage = appearance.Languages[0];
ledger.QuickAmountText = null; ledger.QuickNote = null;
ledger.FromDate = new DateTimeOffset(today.AddDays(-30).ToDateTime(TimeOnly.MinValue));
ledger.ToDate = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue)); ledger.FilterCommand.Execute();
model.SelectedPageIndex = 1;
var deposits = (MemberDepositsPageViewModel)model.CurrentPage!.DataContext!;
deposits.SelectAll(false);
Check(!deposits.CanSettle && !deposits.WithdrawFirstCommand.CanExecute(null), "Clearing selection disables deposit operations");
var selectedDeposit = deposits.DepositRows.Last(row => row.CanOperate);
selectedDeposit.IsSelected = true;
IReadOnlyList<DepositChainView>? selectedForSettlement = null;
deposits.ShowSettleDialogAsync = chains => { selectedForSettlement = chains; return Task.FromResult(false); };
deposits.SettleCommand.Execute(null); Pump();
Check(selectedForSettlement?.Count == 1 && selectedForSettlement[0].HeadId == selectedDeposit.Chain.HeadId, "Settle receives only the selected active chain");
DepositChainView? selectedForWithdrawal = null;
deposits.ShowWithdrawDialogAsync = chain => { selectedForWithdrawal = chain; return Task.FromResult(false); };
deposits.WithdrawFirstCommand.Execute(null); Pump();
Check(selectedForWithdrawal?.HeadId == selectedDeposit.Chain.HeadId, "Toolbar withdrawal follows the selected chain");
var settleResult = AppServices.Deposits.Settle(member.Id, [selectedDeposit.Chain.HeadId], today, admin.Id);
Check(settleResult.DepositCount == 1 && AppServices.Deposits.ListSettlements(member.Id).Single().TotalInterest == settleResult.TotalInterest, "AOT settlement persists its history and selected chain");
var settledChain = AppServices.Deposits.ListChains(member.Id, today).Single(chain => chain.Principal > 4000m && chain.Principal < 5000m);
AppServices.Deposits.Withdraw(member.Id, settledChain.HeadId, 100m, today, "Headless partial withdrawal");
Check(AppServices.Deposits.ListChains(member.Id, today).Any(chain => !chain.IsActive), "AOT partial withdrawal closes the source chain transactionally");
deposits.Refresh();
model.SelectedPageIndex = 0;
foreach (var language in appearance.Languages)
{
    appearance.SelectedLanguage = language; Pump();
    for (var page = 0; page < model.Pages.Count; page++)
    {
        model.SelectedPageIndex = page;
        Capture(window, $"member-{page}-{language.Key}.png");
        CheckLocalized(window);
    }
}
appearance.SelectedLanguage = appearance.Languages[0];
foreach (var palette in appearance.Palettes)
foreach (var mode in appearance.Modes.Where(item => item.Key != "System"))
{
    appearance.SelectedPalette = palette;
    appearance.SelectedMode = mode;
    model.SelectedPageIndex = 0;
    Capture(window, $"ledger-{palette.Key}-{mode.Key}.png");
    var stored = AppearanceService.Read(Path.Combine(AppServices.DataDirectory, "appearance.json"));
    Check(stored.Palette.ToString() == palette.Key && stored.Mode.ToString() == mode.Key, "Preference round trip " + palette.Key + " " + mode.Key);
    Check(Application.Current!.TryGetResource("StudioBackgroundColor", Application.Current.ActualThemeVariant, out var expected) &&
        window.Background is ISolidColorBrush actual && actual.Color == (Color)expected!, "Window follows shared background " + palette.Key + " " + mode.Key);
}
appearance.SelectedMode = appearance.Modes[0];
Check(AppearanceService.Read(Path.Combine(AppServices.DataDirectory, "appearance.json")).Mode == StudioThemeMode.System, "System mode persists independently");
window.Close();
Session.Current = admin;
window = new MainWindow(); window.Show(); model = (MainWindowViewModel)window.DataContext!;
appearance.SelectedPalette = appearance.Palettes[0]; appearance.SelectedMode = appearance.Modes[1];
foreach (var language in appearance.Languages)
{
    appearance.SelectedLanguage = language;
    for (var page = 0; page < model.Pages.Count; page++)
    {
        model.SelectedPageIndex = page;
        Capture(window, $"admin-{page}-{language.Key}.png");
        CheckLocalized(window);
    }
}
var settings = new AppearanceWindow(); settings.Show(window);
model.SelectedPageIndex = 2;
var members = (AdminMembersPageViewModel)model.CurrentPage!.DataContext!;
members.Keyword = "Alex";
Check(members.Users.Count == 1 && members.Users[0].Username == "member", "Member search matches display names");
members.Keyword = "MEMBER2";
Check(members.Users.Count == 1 && members.Users[0].DisplayName == "Sam", "Member search matches usernames without case sensitivity");
members.Keyword = null;
Check(members.Users.Count == 3, "Clearing member search restores the complete list");
Check(settings.GetVisualDescendants().OfType<ComboBox>().Count() == 3, "Appearance exposes three real selectors");
Capture(settings, "appearance-en-US.png"); settings.Close();
var fake = new FakeUpdates();
var opened = new List<string>(); var exits = 0;
using var updates = new UpdateViewModel(fake, opened.Add, () => exits++);
var metadata = new AboutViewModel(updates);
var about = new AboutWindow(metadata); about.Show(window);
foreach (var language in appearance.Languages)
{
    appearance.SelectedLanguage = language;
    Capture(about, "about-" + language.Key + ".png");
    Check(about.Title == AppText.Get(Language.AboutTitle), "Existing About updates its title " + language.Key);
    CheckLocalized(about);
}
Check(metadata.VersionText == UpdateVersion.Display(typeof(AboutViewModel).Assembly), "About uses actual application assembly version");
Check(metadata.AuthorText == "沙漠尽头的狼", "About reads author metadata from the application assembly");
Check(metadata.CompileTimeText != "—" && metadata.PlatformText.Contains("Avalonia"), "Build metadata survives publication");
Complete(updates.CheckAsync()); Check(updates.State == UpdateState.Current, "Latest release state");
fake.Result = UpdateCheckResult.Failed("offline"); Complete(updates.CheckAsync()); Check(updates.State == UpdateState.Failed && updates.StatusText.Contains("offline"), "Check failure remains visible");
fake.Result = new(NewUpdate(), true, null);
Complete(updates.AdvanceAsync()); Check(updates.State == UpdateState.Available, "Failed check retries");
fake.DownloadFailure = true; Complete(updates.AdvanceAsync()); Check(updates.State == UpdateState.Failed, "Download failure displayed");
fake.DownloadFailure = false; Complete(updates.AdvanceAsync()); Check(updates.State == UpdateState.Downloaded, "Download retry preserves the selected release");
fake.InstallFailure = true; Complete(updates.AdvanceAsync()); Check(updates.State == UpdateState.Failed, "Install failure displayed");
fake.InstallFailure = false; Complete(updates.AdvanceAsync()); Check(updates.State == UpdateState.Installed && exits == 1, "Installation retry requests shutdown only after launch");
foreach (var missing in new[] { "asset", "checksum", "both" })
{
    fake.Result = new(NewUpdate() with { AssetUrl = missing is "asset" or "both" ? null : "https://example.test/setup.exe", ChecksumUrl = missing is "checksum" or "both" ? null : "https://example.test/setup.exe.sha256" }, true, null);
    Complete(updates.CheckAsync()); var before = fake.DownloadCount;
    Complete(updates.AdvanceAsync()); Check(opened.Count > 0 && fake.DownloadCount == before, "Missing " + missing + " falls back to release page");
}
fake.Result = new(NewUpdate() with { AssetName = "portable.zip" }, true, null);
Complete(updates.CheckAsync()); Complete(updates.AdvanceAsync());
Check(updates.ActionText == AppText.Get(Language.AboutOpenFolder), "Portable archive offers folder action");
fake.InstallResult = new(UpdateInstallKind.OpenedFolder, "Portable folder opened") { ShouldExitApplication = false };
Complete(updates.AdvanceAsync()); Check(exits == 1, "Portable archive does not exit application");
fake.Delay = true;
var pending = updates.CheckAsync();
Check(updates.IsBusy && !updates.UpdateCommand.CanExecute(null), "Busy controls disable duplicate action");
updates.Cancel(); Complete(pending); Check(updates.State == UpdateState.Canceled, "Cancellation restores a usable state");
var closing = new UpdateViewModel(fake, _ => { }, () => { }); pending = closing.CheckAsync(); closing.Dispose(); Complete(pending);
Check(!closing.UpdateCommand.CanExecute(null), "Closed update model cancels and disables commands");
about.Close(); window.Close();
File.WriteAllLines(Path.Combine(output, "assertions.txt"), assertions);
Console.WriteLine($"HomeLedger verification passed: {assertions.Count} assertions; NativeAOT={!RuntimeFeature.IsDynamicCodeSupported}; {output}");

void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException(description);
    assertions.Add(description); Console.WriteLine("PASS " + description);
}
void Pump()
{
    var timer = Stopwatch.StartNew();
    do { Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Thread.Sleep(8); } while (timer.ElapsedMilliseconds < 100);
}
void Complete(Task task)
{
    var timer = Stopwatch.StartNew();
    while (!task.IsCompleted && timer.ElapsedMilliseconds < 10000) Pump();
    Check(task.IsCompleted, "Async operation completes"); task.GetAwaiter().GetResult(); Pump();
}
void Capture(Window target, string file)
{
    target.UpdateLayout(); Pump();
    using var image = target.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame: " + file);
    using var stream = File.Create(Path.Combine(output, file)); image.Save(stream, new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
}
void CheckLocalized(Window target)
{
    Check(!target.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text?.StartsWith("HomeLedger.", StringComparison.Ordinal) == true), "Visible controls resolve language keys");
    foreach (var chart in target.GetVisualDescendants().OfType<HomeLedger.Avalonia.Controls.BarChartControl>())
        Check(chart.InLabel == AppText.Get(Language.Income) && chart.OutLabel == AppText.Get(Language.Expense), "Chart legends follow the current language");
}
UpdateInfo NewUpdate() => new(new Version(9, 0), "v9.0", "HomeLedger", "Release notes", "https://example.test/release", "https://example.test/setup.exe", "setup.exe", "https://example.test/setup.exe.sha256");

sealed class FakeUpdates : IUpdateService
{
    public UpdateServiceOptions Options { get; } = new() { Repository = "HomeLedger", ProductName = "HomeLedger" };
    public Version CurrentVersion => new(1, 0);
    public string CurrentVersionText => "1.0";
    public UpdateCheckResult Result { get; set; } = UpdateCheckResult.Latest();
    public UpdateInstallResult InstallResult { get; set; } = new(UpdateInstallKind.Launched, "Installer launched") { ShouldExitApplication = true };
    public bool Delay { get; set; }
    public bool DownloadFailure { get; set; }
    public bool InstallFailure { get; set; }
    public int DownloadCount { get; private set; }
    public async Task<UpdateCheckResult> CheckAsync(Version? current = null, CancellationToken cancellationToken = default)
    {
        if (Delay) await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return Result;
    }
    public Task<UpdateDownloadResult> DownloadAsync(UpdateInfo update, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        DownloadCount++;
        if (DownloadFailure) throw new IOException("Checksum mismatch");
        progress?.Report(new(100, 100));
        return Task.FromResult(new UpdateDownloadResult("verified-package", update.AssetName!, 100));
    }
    public Task<UpdateInstallResult> InstallAsync(UpdateDownloadResult download, CancellationToken cancellationToken = default)
    {
        if (InstallFailure) throw new IOException("Installer failed");
        return Task.FromResult(InstallResult);
    }
}
