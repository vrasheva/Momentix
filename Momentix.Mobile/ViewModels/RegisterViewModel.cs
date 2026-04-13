using CommunityToolkit.Mvvm.Input;
using Momentix.Mobile.Services;

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

        public RegisterViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        public IRelayCommand RegisterCommand => new AsyncRelayCommand(Register);
        public IRelayCommand GoToLoginCommand => new AsyncRelayCommand(GoToLogin);

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
                var result = await _apiService.PostAsync<Models.AuthResponse>("Auth/register", new
                {
                    fullName = FullName,
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
                    ErrorMessage = "Грешка при регистрацията. Опитай отново.";
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

        private async Task GoToLogin()
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}