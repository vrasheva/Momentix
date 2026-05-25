using CommunityToolkit.Mvvm.Input;
using Momentix.Mobile.Services;
using Momentix.Data.DTOs;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace Momentix.Mobile.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

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

        public LoginViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        public IRelayCommand LoginCommand => new AsyncRelayCommand(Login);
        public IRelayCommand GoToRegisterCommand => new AsyncRelayCommand(GoToRegister);

        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(UserEmail) || string.IsNullOrWhiteSpace(UserPassword))
            {
                ErrorMessage = "Моля въведи имейл и парола.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _apiService.PostAsync<LoginDto, AuthResponseDto>(
                    "Auth/login",
                    new LoginDto
                    {
                        Email = UserEmail,
                        Password = UserPassword
                    });

                if (result != null)
                {
                    _apiService.ClearToken();
                    _apiService.SetToken(result.Token);
                    Preferences.Set("auth_token", result.Token);
                    Preferences.Set("user_name", result.FullName);
                    Preferences.Set("user_id", result.UserId);
                    Preferences.Set("user_email", UserEmail);
                    Preferences.Set("theme_name", result.ThemeColor);
                    ThemeService.Instance.ApplyTheme(result.ThemeColor, false);

                    await Shell.Current.GoToAsync("//AlbumsPage");
                    await Task.Delay(300);
                    // OnNavigatedTo ще зареди албумите с правилния токен
                }
                else
                {
                    ErrorMessage = "Невалиден имейл или парола.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.ToString();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GoToRegister()
        {
            await Shell.Current.GoToAsync("//RegisterPage");
        }
    }
}