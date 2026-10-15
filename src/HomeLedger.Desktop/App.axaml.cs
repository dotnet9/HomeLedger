using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;
using HomeLedger.Avalonia.Views;
using HomeLedger.Avalonia.Services;

namespace HomeLedger.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        AppearanceService.Initialize(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new LoginWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
