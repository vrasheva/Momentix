using Momentix.Mobile.ViewModels;

namespace Momentix.Mobile.Views;

public partial class AlbumsPage : ContentPage
{
    private readonly AlbumsViewModel _viewModel;

    public AlbumsPage(AlbumsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.LoadAlbumsCommand.CanExecute(null))
            _viewModel.LoadAlbumsCommand.Execute(null);
    }
}