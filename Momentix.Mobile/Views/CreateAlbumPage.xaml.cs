using Momentix.Mobile.ViewModels;

namespace Momentix.Mobile.Views;

public partial class CreateAlbumPage : ContentPage
{
    private readonly CreateAlbumViewModel _viewModel;

    public CreateAlbumPage(CreateAlbumViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    private void OnFriendToggled(object sender, ToggledEventArgs e)
    {
        if (sender is Switch sw && sw.BindingContext is FriendInviteViewModel friend)
        {
            friend.IsInvited = e.Value;
            System.Diagnostics.Debug.WriteLine($"Toggled {friend.FullName}: {e.Value}");
        }
    }
}