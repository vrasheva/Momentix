using Momentix.Mobile.ViewModels;

namespace Momentix.Mobile.Views;

public partial class TimeCapsuleDetailsPage : ContentPage
{
    private readonly TimeCapsuleDetailsViewModel _viewModel;

    public TimeCapsuleDetailsPage(TimeCapsuleDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.PageAppeared();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PageDisappeared();
    }
}
