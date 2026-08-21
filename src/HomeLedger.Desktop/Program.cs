using Avalonia;
using System.Runtime.Versioning;

[assembly: TargetPlatform("Windows10.0.19041.0")]

namespace HomeLedger.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
