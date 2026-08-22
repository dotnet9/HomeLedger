using Avalonia.Controls;
using HomeLedger.Avalonia.ViewModels;
using HomeLedger.Avalonia.Views.Dialogs;

namespace HomeLedger.Avalonia.Views.Pages;

public partial class AdminMembersPage : UserControl
{
    public AdminMembersPage()
    {
        InitializeComponent();
        var viewModel = new AdminMembersPageViewModel(AppServices.Auth, AppServices.Db)
        {
            ShowCreateUserDialogAsync = ShowCreateUserDialogAsync
        };
        DataContext = viewModel;
    }

    private async Task<bool> ShowCreateUserDialogAsync()
    {
        var dialog = new CreateUserDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        return await dialog.ShowDialog<bool>(Owner());
    }

    private Window Owner() => VisualRoot as Window ?? throw new InvalidOperationException();
}
