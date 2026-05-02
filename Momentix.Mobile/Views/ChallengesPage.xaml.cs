using Momentix.Mobile.ViewModels;

namespace Momentix.Mobile.Views;

public partial class ChallengesPage : ContentPage
{
    private readonly ChallengesViewModel _viewModel;

    public ChallengesPage(ChallengesViewModel viewModel)
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
