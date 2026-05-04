using Momentix.Mobile.ViewModels;

namespace Momentix.Mobile.Views;

public partial class FriendsPage : ContentPage
{
    private readonly FriendsViewModel _viewModel;

    public FriendsPage(FriendsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.LoadFriendsCommand.CanExecute(null))
            _viewModel.LoadFriendsCommand.Execute(null);
    }
}
