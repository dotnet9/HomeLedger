using Avalonia;
using Avalonia.Markup.Xaml;
using HomeLedger.Avalonia.Views;
using Prism.DryIoc;
using Prism.Ioc;

namespace HomeLedger.Desktop;

public partial class App : PrismApplication
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    protected override AvaloniaObject CreateShell()
    {
        return Container.Resolve<LoginWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<LoginWindow>();
        containerRegistry.Register<MainWindow>();
    }
}
