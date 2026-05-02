using Momentix.Mobile.ViewModels;

namespace Momentix.Mobile.Views;

public partial class TimeCapsulesPage : ContentPage
{
    private readonly TimeCapsulesViewModel _viewModel;

    public TimeCapsulesPage(TimeCapsulesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.LoadCapsulesCommand.CanExecute(null))
            _viewModel.LoadCapsulesCommand.Execute(null);

        _viewModel.StartCountdown();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopCountdown();
    }
}
