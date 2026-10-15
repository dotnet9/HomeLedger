using System.Reflection;
using System.Runtime.InteropServices;
using CodeWF.Toolkit.Core.Extensions;
using CodeWF.Toolkit.Core.UpdateChecking;
using Prism.Commands;

namespace HomeLedger.Avalonia.ViewModels;

public sealed class AboutViewModel
{
    public AboutViewModel(UpdateViewModel? update = null)
    {
        Update = update ?? new();
        var assembly = typeof(AboutViewModel).Assembly;
        VersionText = UpdateVersion.Display(assembly);
        PlatformText = $"{RuntimeInformation.FrameworkDescription} · Avalonia {typeof(global::Avalonia.Application).Assembly.GetName().Version}";
        CompileTimeText = assembly.CompileTime()?.ToString("yyyy-MM-dd HH:mm:ss", AppText.Culture) ?? "—";
        CopyrightText = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
        AuthorText = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? AppText.Get(Language.AboutAuthorName);
        CloseCommand = new DelegateCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
    }
    public UpdateViewModel Update { get; }
    public string VersionText { get; }
    public string PlatformText { get; }
    public string CompileTimeText { get; }
    public string CopyrightText { get; }
    public string AuthorText { get; }
    public DelegateCommand CloseCommand { get; }
    public event EventHandler? RequestClose;
}
