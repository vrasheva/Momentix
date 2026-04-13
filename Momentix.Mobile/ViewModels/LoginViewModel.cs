using CommunityToolkit.Mvvm.Input;
using Momentix.Mobile.Services;

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
                var result = await _apiService.PostAsync<Models.AuthResponse>("Auth/login", new
                {
                    email = UserEmail,
                    password = UserPassword
                });

                if (result != null)
                {
                    _apiService.SetToken(result.Token);
                    Preferences.Set("auth_token", result.Token);
                    Preferences.Set("user_name", result.FullName);
                    Preferences.Set("user_id", result.UserId);

                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    ErrorMessage = "Невалиден имейл или парола.";
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Грешка при свързване със сървъра.";
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