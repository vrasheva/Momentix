using CommunityToolkit.Mvvm.Input;
using Momentix.Mobile.Services;

namespace Momentix.Mobile.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    private string _fullName = Preferences.Get("user_name", "");
    public string FullName
    {
        get => _fullName;
        set { _fullName = value; OnPropertyChanged(); OnPropertyChanged(nameof(Initials)); }
    }

    private string _email = Preferences.Get("user_email", "");
    public string Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); }
    }

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FullName)) return "?";
            var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
            return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
        }
    }

    private string? _profilePictureUrl;
    public string? ProfilePictureUrl
    {
        get => _profilePictureUrl;
        set
        {
            _profilePictureUrl = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasProfilePicture));
            OnPropertyChanged(nameof(HasNoProfilePicture));
        }
    }

    public bool HasProfilePicture => !string.IsNullOrWhiteSpace(ProfilePictureUrl);
    public bool HasNoProfilePicture => !HasProfilePicture;

    private bool _isEditingName;
    public bool IsEditingName
    {
        get => _isEditingName;
        set { _isEditingName = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotEditingName)); }
    }
    public bool IsNotEditingName => !IsEditingName;

    private bool _isEditingEmail;
    public bool IsEditingEmail
    {
        get => _isEditingEmail;
        set { _isEditingEmail = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotEditingEmail)); }
    }
    public bool IsNotEditingEmail => !IsEditingEmail;

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public string SelectedTheme => Preferences.Get("theme_name", "Blue");

    public IRelayCommand BackCommand => new AsyncRelayCommand(async () =>
        await Shell.Current.GoToAsync(".."));

    public IRelayCommand EditNameCommand => new RelayCommand(() => IsEditingName = true);

    public IRelayCommand SaveNameCommand => new AsyncRelayCommand(async () =>
    {
        if (string.IsNullOrWhiteSpace(FullName)) return;
        Preferences.Set("user_name", FullName);
        IsEditingName = false;
        StatusMessage = "Името е запазено.";
        await Task.Delay(2000);
        StatusMessage = string.Empty;
    });

    public IRelayCommand EditEmailCommand => new RelayCommand(() => IsEditingEmail = true);

    public IRelayCommand SaveEmailCommand => new AsyncRelayCommand(async () =>
    {
        if (string.IsNullOrWhiteSpace(Email)) return;
        Preferences.Set("user_email", Email);
        IsEditingEmail = false;
        StatusMessage = "Имейлът е запазен.";
        await Task.Delay(2000);
        StatusMessage = string.Empty;
    });

    public IRelayCommand<string> SelectThemeCommand => new RelayCommand<string>(theme =>
    {
        if (string.IsNullOrEmpty(theme)) return;
        ThemeService.Instance.ApplyTheme(theme, false);
        OnPropertyChanged(nameof(SelectedTheme));
    });

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
            ProfilePictureUrl = file.FullPath;
        }
        catch { }
    });

    public ProfileViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }
}