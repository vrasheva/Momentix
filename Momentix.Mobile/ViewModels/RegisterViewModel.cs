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

        private string _emailStatusMessage = string.Empty;
        public string EmailStatusMessage
        {
            get => _emailStatusMessage;
            set { _emailStatusMessage = value; OnPropertyChanged(); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private bool _isSendingEmail = false;
        public bool IsSendingEmail
        {
            get => _isSendingEmail;
            set
            {
                _isSendingEmail = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SendEmailButtonText));
            }
        }

        public string SendEmailButtonText => IsSendingEmail ? "Sending email..." : "Send test email";

        private string _selectedTheme = "Black";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set { _selectedTheme = value; OnPropertyChanged(); }
        }

        public RegisterViewModel(ApiService apiService)
        {
            _apiService = apiService;
            ThemeService.Instance.ApplyTheme("Black", false);
        }

        public IRelayCommand RegisterCommand => new AsyncRelayCommand(Register);
        public IRelayCommand SendRegistrationEmailCommand => new AsyncRelayCommand(SendRegistrationEmail);
        public IRelayCommand GoToLoginCommand => new AsyncRelayCommand(GoToLogin);
        public IRelayCommand<string> SelectThemeCommand => new RelayCommand<string>(SelectTheme);

        private void SelectTheme(string? theme)
        {
            if (string.IsNullOrEmpty(theme)) return;

            var xpRequired = ThemeService.ThemeXpRequired;
            if (!xpRequired.ContainsKey(theme)) return;
            if (xpRequired[theme] > 0) return;

            SelectedTheme = theme;
            ThemeService.Instance.ApplyTheme(theme, false);
        }

        private async Task SendRegistrationEmail()
        {
            if (IsSendingEmail) return;

            if (string.IsNullOrWhiteSpace(UserEmail))
            {
                ErrorMessage = "Enter an email first.";
                EmailStatusMessage = string.Empty;
                return;
            }

            IsSendingEmail = true;
            ErrorMessage = string.Empty;
            EmailStatusMessage = string.Empty;

            try
            {
                var success = await _apiService.PostAsync(
                    "Auth/send-registration-email",
                    new SendRegistrationEmailDto
                    {
                        Email = UserEmail,
                        FullName = FullName
                    });

                if (success)
                    EmailStatusMessage = "Test email sent.";
                else
                    ErrorMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                        ? "Email could not be sent."
                        : _apiService.LastErrorMessage;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Email error: {ex.Message}";
            }
            finally
            {
                IsSendingEmail = false;
            }
        }

        private async Task Register()
        {
            if (string.IsNullOrWhiteSpace(FullName) ||
                string.IsNullOrWhiteSpace(UserEmail) ||
                string.IsNullOrWhiteSpace(UserPassword))
            {
                ErrorMessage = "Please fill all fields.";
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
                        ? "Registration failed. Try again."
                        : _apiService.LastErrorMessage;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
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
