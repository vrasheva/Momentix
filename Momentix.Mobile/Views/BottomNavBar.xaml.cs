using Momentix.Mobile.Services;

namespace Momentix.Mobile.Views;

public enum NavTab { Albums, Friends, Capsules, Challenge }

public partial class BottomNavBar : ContentView
{
    private static readonly Color Inactive = Color.FromArgb("#AAAAAA");

    private Color GetActiveColor()
    {
        var theme = ThemeService.Instance.CurrentTheme;
        if (ThemeService.Themes.ContainsKey(theme))
            return ThemeService.Themes[theme]["Primary"];
        return Color.FromArgb("#6750A4");
    }

    public NavTab ActiveTab
    {
        get => GetCurrentTab();
        set { UpdateVisuals(); }
    }

    public BottomNavBar()
    {
        InitializeComponent();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        UpdateVisuals();
        Shell.Current.Navigated += OnShellNavigated;
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        UpdateVisuals();
    }

    private NavTab GetCurrentTab()
    {
        var location = Shell.Current?.CurrentState?.Location?.ToString() ?? "";
        if (location.Contains("FriendsPage")) return NavTab.Friends;
        if (location.Contains("TimeCapsulesPage")) return NavTab.Capsules;
        if (location.Contains("ChallengesPage")) return NavTab.Challenge;
        return NavTab.Albums;
    }

    private void UpdateVisuals()
    {
        if (FntAlbums == null) return;

        var tab = GetCurrentTab();
        var activeColor = GetActiveColor();
        var activeBrush = new SolidColorBrush(activeColor);

        FntAlbums.Color = Inactive;
        FntFriends.Color = Inactive;
        FntCapsules.Color = Inactive;
        FntChallenge.Color = Inactive;

        DotAlbums.IsVisible = false;
        DotFriends.IsVisible = false;
        DotCapsules.IsVisible = false;
        DotChallenge.IsVisible = false;

        switch (tab)
        {
            case NavTab.Albums:
                FntAlbums.Color = activeColor;
                DotAlbums.IsVisible = true;
                DotAlbums.Fill = activeBrush;
                break;
            case NavTab.Friends:
                FntFriends.Color = activeColor;
                DotFriends.IsVisible = true;
                DotFriends.Fill = activeBrush;
                break;
            case NavTab.Capsules:
                FntCapsules.Color = activeColor;
                DotCapsules.IsVisible = true;
                DotCapsules.Fill = activeBrush;
                break;
            case NavTab.Challenge:
                FntChallenge.Color = activeColor;
                DotChallenge.IsVisible = true;
                DotChallenge.Fill = activeBrush;
                break;
        }
    }

    private async void OnAlbumsTapped(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//AlbumsPage");

    private async void OnFriendsTapped(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//FriendsPage");

    private async void OnCapsulesTapped(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//TimeCapsulesPage");

    private async void OnChallengeTapped(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//ChallengesPage");
}