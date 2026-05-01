using Momentix.Mobile.ViewModels;

namespace Momentix.Mobile.Views;

public partial class CreateAlbumPage : ContentPage
{
    public CreateAlbumPage(CreateAlbumViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}