using Momentix.Mobile.ViewModels;

namespace Momentix.Mobile.Views;

public partial class CreateTimeCapsulePage : ContentPage
{
    public CreateTimeCapsulePage(CreateTimeCapsuleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
