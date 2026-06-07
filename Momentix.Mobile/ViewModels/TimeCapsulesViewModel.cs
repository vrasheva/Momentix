using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

public partial class TimeCapsulesViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private IDispatcherTimer? _timer;

    public ObservableCollection<TimeCapsuleItemViewModel> Capsules { get; } = new();
    public ObservableCollection<TimeCapsuleItemViewModel> RestCapsules { get; } = new();
    public bool HasFirstCapsule => Capsules.Count > 0;
    public TimeCapsuleItemViewModel? FirstCapsule => Capsules.Count > 0 ? Capsules[0] : null;

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public IRelayCommand LoadCapsulesCommand => new AsyncRelayCommand(LoadCapsules);
    public IRelayCommand GoToCreateCapsuleCommand => new AsyncRelayCommand(GoToCreateCapsule);
    public IRelayCommand<TimeCapsuleItemViewModel> OpenCapsuleCommand => new AsyncRelayCommand<TimeCapsuleItemViewModel>(OpenCapsule);
    public IRelayCommand LogoutCommand => new AsyncRelayCommand(Logout);
    public IRelayCommand GoToProfileCommand => new AsyncRelayCommand(async () =>
        await Shell.Current.GoToAsync("ProfilePage"));

    public TimeCapsulesViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public void StartCountdown()
    {
        _timer ??= Application.Current?.Dispatcher.CreateTimer();

        if (_timer == null || _timer.IsRunning)
            return;

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) =>
        {
            foreach (var capsule in Capsules)
                capsule.RefreshCountdown();
        };
        _timer.Start();
    }

    public void StopCountdown()
    {
        _timer?.Stop();
    }

    private async Task LoadCapsules()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _apiService.GetAsync<List<TimeCapsuleResponseDto>>("TimeCapsule");
            Capsules.Clear();
            RestCapsules.Clear();

            if (result == null)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Capsules could not be loaded. Login again and make sure the API is running."
                    : _apiService.LastErrorMessage;
                OnPropertyChanged(nameof(HasFirstCapsule));
                OnPropertyChanged(nameof(FirstCapsule));
                return;
            }

            if (result.Count == 0)
            {
                OnPropertyChanged(nameof(HasFirstCapsule));
                OnPropertyChanged(nameof(FirstCapsule));
                return;
            }

            foreach (var capsule in result
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .OrderBy(c => c.UnlockAt))
                Capsules.Add(new TimeCapsuleItemViewModel(capsule));

            foreach (var capsule in Capsules.Skip(1))
                RestCapsules.Add(capsule);

            OnPropertyChanged(nameof(HasFirstCapsule));
            OnPropertyChanged(nameof(FirstCapsule));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task GoToCreateCapsule()
    {
        try
        {
            await Shell.Current.GoToAsync("CreateTimeCapsulePage");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task OpenCapsule(TimeCapsuleItemViewModel? capsule)
    {
        if (capsule == null)
            return;

        try
        {
            await Shell.Current.GoToAsync(
                $"TimeCapsuleDetailsPage?TimeCapsuleId={capsule.Id}&CapsuleTitle={Uri.EscapeDataString(capsule.Title)}");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task Logout()
    {
        Preferences.Remove("auth_token");
        Preferences.Remove("user_name");
        Preferences.Remove("user_id");
        _apiService.ClearToken();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}

public class TimeCapsuleItemViewModel : BaseViewModel
{
    private readonly TimeCapsuleResponseDto _capsule;

    public int Id => _capsule.Id;
    public string Title => _capsule.Title;
    public string? Description => _capsule.Description;
    public string OwnerName => _capsule.OwnerName;
    public int MemberCount => _capsule.MemberCount;
    public int MediaCount => _capsule.MediaCount;
    public bool IsUnlocked => _capsule.IsUnlocked || DateTime.UtcNow >= _capsule.UnlockAt;
    public bool IsLocked => !IsUnlocked;
    public string StatusText => IsUnlocked ? "Отключена" : "Заключена";
    public string UnlockDateText => $"Отключва се: {_capsule.UnlockAt.ToLocalTime():dd.MM.yyyy HH:mm}";
    public string UnlockDateShort => _capsule.UnlockAt.ToLocalTime().ToString("dd.MM.yyyy");

    public string CardBackground
    {
        get
        {
            var theme = Preferences.Get("theme_name", "Blue");
            return theme switch
            {
                "Blue" => "#DBEAFE",
                "Green" => "#D1FAE5",
                "Yellow" => "#FEF3C7",
                "Purple" => "#EDE9FE",
                "Black" => "#F3F4F6",
                _ => "#DBEAFE"
            };
        }
    }

    public string CountdownText
    {
        get
        {
            if (IsUnlocked) return "Ready to open";
            var remaining = _capsule.UnlockAt - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            if (remaining.TotalDays >= 1)
                return $"{(int)remaining.TotalDays}д {remaining.Hours}ч";
            return $"{remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }

    public TimeCapsuleItemViewModel(TimeCapsuleResponseDto capsule)
    {
        _capsule = capsule;
    }

    public void RefreshCountdown()
    {
        OnPropertyChanged(nameof(IsUnlocked));
        OnPropertyChanged(nameof(IsLocked));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(CountdownText));
        OnPropertyChanged(nameof(CardBackground));
    }
}