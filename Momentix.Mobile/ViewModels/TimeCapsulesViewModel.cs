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

            if (result == null)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Capsules could not be loaded. Login again and make sure the API is running."
                    : _apiService.LastErrorMessage;
                return;
            }

            if (result.Count == 0)
            {
                ErrorMessage = "No capsules yet. Create one with the New Capsule button.";
                return;
            }

            foreach (var capsule in result
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .OrderBy(c => c.UnlockAt))
                Capsules.Add(new TimeCapsuleItemViewModel(capsule));
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
            var parameters = new Dictionary<string, object>
            {
                ["TimeCapsuleId"] = capsule.Id,
                ["CapsuleTitle"] = capsule.Title
            };

            await Shell.Current.GoToAsync("TimeCapsuleDetailsPage", parameters);
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
    public string StatusText => IsUnlocked ? "Unlocked" : "Locked";
    public string UnlockDateText => $"Unlocks: {_capsule.UnlockAt.ToLocalTime():dd.MM.yyyy HH:mm}";

    public string CountdownText
    {
        get
        {
            if (IsUnlocked)
                return "Ready to open";

            var remaining = _capsule.UnlockAt - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            return $"{remaining.Days}d {remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }

    public TimeCapsuleItemViewModel(TimeCapsuleResponseDto capsule)
    {
        _capsule = capsule;
    }

    public void RefreshCountdown()
    {
        OnPropertyChanged(nameof(IsUnlocked));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(CountdownText));
    }
}
