using Momentix.Mobile.ViewModels;

namespace Momentix.Mobile.Views;

public partial class AlbumDetailsPage : ContentPage
{
    private readonly AlbumDetailsViewModel _viewModel;

    public AlbumDetailsPage(AlbumDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.LoadCommand.CanExecute(null))
            _viewModel.LoadCommand.Execute(null);
    }
}
