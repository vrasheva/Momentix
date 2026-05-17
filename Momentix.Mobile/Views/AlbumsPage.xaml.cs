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

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _viewModel.LoadAlbumsCommand.Execute(null);
    }
}