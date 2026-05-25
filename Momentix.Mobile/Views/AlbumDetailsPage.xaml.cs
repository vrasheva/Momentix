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
        _viewModel.PageAppeared();
    }

    private void SetActiveTab(int tab)
    {
        TabPhotos.TextColor = Color.FromArgb("#888");
        TabPhotos.FontAttributes = FontAttributes.None;
        TabMemories.TextColor = Color.FromArgb("#888");
        TabMemories.FontAttributes = FontAttributes.None;
        TabMembers.TextColor = Color.FromArgb("#888");
        TabMembers.FontAttributes = FontAttributes.None;

        UnderlinePhotos.BackgroundColor = Colors.Transparent;
        UnderlineMemories.BackgroundColor = Colors.Transparent;
        UnderlineMembers.BackgroundColor = Colors.Transparent;

        PhotosContent.IsVisible = false;
        MemoriesContent.IsVisible = false;
        MembersContent.IsVisible = false;

        switch (tab)
        {
            case 0:
                TabPhotos.TextColor = Color.FromArgb("#111");
                TabPhotos.FontAttributes = FontAttributes.Bold;
                UnderlinePhotos.BackgroundColor = Color.FromArgb("#111");
                PhotosContent.IsVisible = true;
                break;
            case 1:
                TabMemories.TextColor = Color.FromArgb("#111");
                TabMemories.FontAttributes = FontAttributes.Bold;
                UnderlineMemories.BackgroundColor = Color.FromArgb("#111");
                MemoriesContent.IsVisible = true;
                break;
            case 2:
                TabMembers.TextColor = Color.FromArgb("#111");
                TabMembers.FontAttributes = FontAttributes.Bold;
                UnderlineMembers.BackgroundColor = Color.FromArgb("#111");
                MembersContent.IsVisible = true;
                break;
        }
    }

    private void OnPhotosTapped(object sender, EventArgs e) => SetActiveTab(0);
    private void OnMemoriesTapped(object sender, EventArgs e) => SetActiveTab(1);
    private void OnMembersTapped(object sender, EventArgs e) => SetActiveTab(2);

    private void OnPopupContentTapped(object sender, TappedEventArgs e)
    {
    }
}