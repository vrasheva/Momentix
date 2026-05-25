using CommunityToolkit.Mvvm.Input;
using Momentix.Mobile.Services;

namespace Momentix.Mobile.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    public string FullName => Preferences.Get("user_name", "");
    public string Email => Preferences.Get("user_email", "");

    private string? _profilePictureUrl;
    public string? ProfilePictureUrl
    {
        get => _profilePictureUrl;
        set { _profilePictureUrl = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasProfilePicture)); OnPropertyChanged(nameof(HasNoProfilePicture)); }
    }

    public bool HasProfilePicture => !string.IsNullOrWhiteSpace(ProfilePictureUrl);
    public bool HasNoProfilePicture => !HasProfilePicture;

    public IRelayCommand BackCommand => new AsyncRelayCommand(async () =>
        await Shell.Current.GoToAsync(".."));

    public IRelayCommand LogoutCommand => new AsyncRelayCommand(async () =>
    {
        Preferences.Remove("auth_token");
        Preferences.Remove("user_name");
        Preferences.Remove("user_email");
        Preferences.Remove("user_id");
        _apiService.ClearToken();
        await Shell.Current.GoToAsync("//LoginPage");
    });

    public IRelayCommand PickProfilePictureCommand => new AsyncRelayCommand(async () =>
    {
        try
        {
            var file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Избери профилна снимка"
            });
            if (file == null) return;

            // TODO: качи снимката към API когато имаш endpoint
            // Засега само показваме локално
            ProfilePictureUrl = file.FullPath;
        }
        catch { }
    });

    public ProfileViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }
}