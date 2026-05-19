using CommunityToolkit.Mvvm.Input;
using Momentix.Mobile.Services;
using Momentix.Data.DTOs;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace Momentix.Mobile.ViewModels
{
    public partial class RegisterViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        private string _userEmail = string.Empty;
        public string UserEmail
        {
            get => _userEmail;
            set { _userEmail = value; OnPropertyChanged(); }
        }

        private string _userPassword = string.Empty;
        public string UserPassword
        {
            get => _userPassword;
            set { _userPassword = value; OnPropertyChanged(); }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string _selectedTheme = "Purple";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set { _selectedTheme = value; OnPropertyChanged(); }
        }

        public RegisterViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        public IRelayCommand RegisterCommand => new AsyncRelayCommand(Register);
        public IRelayCommand GoToLoginCommand => new AsyncRelayCommand(GoToLogin);
        public IRelayCommand<string> SelectThemeCommand => new RelayCommand<string>(SelectTheme);

        private void SelectTheme(string? theme)
        {
            if (string.IsNullOrEmpty(theme)) return;

            var xpRequired = ThemeService.ThemeXpRequired;
            if (!xpRequired.ContainsKey(theme)) return;
            if (xpRequired[theme] > 0) return; // само безплатни теми при регистрация

            SelectedTheme = theme;
            ThemeService.Instance.ApplyTheme(theme, false);
        }

        private async Task Register()
        {
            if (string.IsNullOrWhiteSpace(FullName) ||
                string.IsNullOrWhiteSpace(UserEmail) ||
                string.IsNullOrWhiteSpace(UserPassword))
            {
                ErrorMessage = "Моля попълни всички полета.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _apiService.PostAsync<RegisterDto, AuthResponseDto>(
                    "Auth/register",
                    new RegisterDto
                    {
                        FullName = FullName,
                        Email = UserEmail,
                        Password = UserPassword,
                        ThemeColor = SelectedTheme
                    });

                if (result != null)
                {
                    _apiService.SetToken(result.Token);
                    Preferences.Set("auth_token", result.Token);
                    Preferences.Set("user_name", result.FullName);
                    Preferences.Set("user_id", result.UserId);
                    Preferences.Set("user_xp", 0);
                    Preferences.Set("theme_name", result.ThemeColor);
                    ThemeService.Instance.ApplyTheme(SelectedTheme, false);
                    await Shell.Current.GoToAsync("//AlbumsPage");
                }
                else
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                        ? "Грешка при регистрацията. Опитай отново."
                        : _apiService.LastErrorMessage;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Грешка: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GoToLogin()
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}