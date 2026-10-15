using Avalonia.Controls;
using Avalonia.Interactivity;
using CodeWF.Avalonia.Controls.Controls;
using HomeLedger.Avalonia.Services;
using HomeLedger.Avalonia.ViewModels;

namespace HomeLedger.Avalonia.Views;

public partial class MainWindow : CodeWFWindow
{
    private AboutWindow? _about;
    private AppearanceWindow? _appearance;
    public MainWindow()
    {
        InitializeComponent();
        WindowChrome.Apply(this, Language.AppTitle, themeControls: true);
        var user = Session.Current ?? throw new InvalidOperationException(AppText.Get(Language.NotSignedIn));
        DataContext = new MainWindowViewModel(user);
    }
    private async void OnChangePassword(object? sender, RoutedEventArgs e) => await new Dialogs.ChangePasswordDialog().ShowDialog<bool>(this);
    private void OnCheckUpdate(object? sender, RoutedEventArgs e)
    {
        if (_about != null) { _about.Activate(); return; }
        _about = new AboutWindow();
        _about.Closed += (_, _) => _about = null;
        _about.Show(this);
    }
    private void OnAppearance(object? sender, RoutedEventArgs e)
    {
        if (_appearance != null) { _appearance.Activate(); return; }
        _appearance = new AppearanceWindow();
        _appearance.Closed += (_, _) => _appearance = null;
        _appearance.Show(this);
    }
    private void OnLogout(object? sender, RoutedEventArgs e)
    {
        Session.Current = null;
        new LoginWindow().Show();
        Close();
    }
    protected override void OnClosed(EventArgs args)
    {
        _about?.Close();
        _appearance?.Close();
        (DataContext as MainWindowViewModel)?.Dispose();
        base.OnClosed(args);
    }
}
