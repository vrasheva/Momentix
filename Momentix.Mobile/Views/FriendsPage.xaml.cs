using Momentix.Mobile.ViewModels;

namespace Momentix.Mobile.Views;

public partial class FriendsPage : ContentPage
{
    public FriendsPage(FriendsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetNavBarIsVisible(this, false);
        if (BindingContext is FriendsViewModel vm)
            vm.LoadFriendsCommand.Execute(null);
    }

    private void OnPopupContentTapped(object sender, TappedEventArgs e)
    {
    }
}