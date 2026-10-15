using Avalonia.Interactivity;
using CodeWF.Avalonia.Controls.Controls;
using HomeLedger.Avalonia.Services;

namespace HomeLedger.Avalonia.Views;

public partial class AppearanceWindow : CodeWFWindow
{
    public AppearanceWindow()
    {
        InitializeComponent();
        DataContext = AppearanceService.Current;
    }
    private void OnClose(object? sender, RoutedEventArgs args) => Close();
}
