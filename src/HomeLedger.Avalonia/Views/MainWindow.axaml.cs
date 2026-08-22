using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using HomeLedger.Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using HomeLedger.Avalonia.ViewModels;
using HomeLedger.Core.Models;

namespace HomeLedger.Avalonia.Views;

public partial class MainWindow : JadeWindow
{
    public MainWindow()
    {
        InitializeComponent();
        LeftContent = new TextBlock { Text = "HomeLedger 家庭记账", FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#2b2f2c"), Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        var user = Session.Current ?? throw new InvalidOperationException("未登录");
        DataContext = new MainWindowViewModel(user);
    }

    private async void OnChangePassword(object? sender, RoutedEventArgs e)
    {
        var dialog = new Dialogs.ChangePasswordDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        await dialog.ShowDialog<bool>(this);
    }
}
