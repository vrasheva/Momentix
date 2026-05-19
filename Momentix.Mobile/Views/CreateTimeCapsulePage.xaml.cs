using Momentix.Mobile.ViewModels;

namespace Momentix.Mobile.Views;

public partial class CreateTimeCapsulePage : ContentPage
{
    private readonly CreateTimeCapsuleViewModel _viewModel;

    public CreateTimeCapsulePage(CreateTimeCapsuleViewModel viewModel)
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
}