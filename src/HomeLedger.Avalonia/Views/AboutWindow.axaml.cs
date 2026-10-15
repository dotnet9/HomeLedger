using CodeWF.Avalonia.Controls.Controls;
using HomeLedger.Avalonia.ViewModels;

namespace HomeLedger.Avalonia.Views;

public partial class AboutWindow : CodeWFWindow
{
    public AboutWindow() : this(new AboutViewModel()) { }
    public AboutWindow(AboutViewModel model)
    {
        InitializeComponent();
        DataContext = model;
        model.RequestClose += OnRequestClose;
    }
    private void OnRequestClose(object? sender, EventArgs args) => Close();
    protected override void OnClosed(EventArgs args)
    {
        if (DataContext is AboutViewModel model) { model.RequestClose -= OnRequestClose; model.Update.Dispose(); }
        base.OnClosed(args);
    }
}
