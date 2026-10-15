using Avalonia.Controls;
using HomeLedger.Avalonia.Services;

namespace HomeLedger.Avalonia.Views;

public partial class TitleBarAppearance : UserControl
{
    public TitleBarAppearance()
    {
        InitializeComponent();
        DataContext = AppearanceService.Current;
    }
}
