using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

[QueryProperty(nameof(TimeCapsuleId), nameof(TimeCapsuleId))]
[QueryProperty(nameof(CapsuleTitle), nameof(CapsuleTitle))]
public partial class TimeCapsuleDetailsViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private IDispatcherTimer? _timer;
    private TimeCapsuleResponseDto? _capsule;
    private bool _mediaLoaded;

    public ObservableCollection<AlbumMediaItemViewModel> MediaItems { get; } = new();

    private int _timeCapsuleId;
    public int TimeCapsuleId
    {
        get => _timeCapsuleId;
        set { _timeCapsuleId = value; OnPropertyChanged(); }
    }

    private string _capsuleTitle = string.Empty;
    public string CapsuleTitle
    {
        get => _capsuleTitle;
        set { _capsuleTitle = value; OnPropertyChanged(); }
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public bool IsUnlocked => _capsule != null && (_capsule.IsUnlocked || DateTime.UtcNow >= _capsule.UnlockAt);
    public bool IsLocked => !IsUnlocked;
    public string StatusText => IsUnlocked ? "Unlocked" : "Locked";
    public string UnlockDateText => _capsule == null
        ? string.Empty
        : $"Unlocks: {_capsule.UnlockAt.ToLocalTime():dd.MM.yyyy HH:mm}";

    public string CountdownText
    {
        get
        {
            if (_capsule == null)
                return string.Empty;

            if (IsUnlocked)
                return "Ready to open";

            var remaining = _capsule.UnlockAt - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            return $"{remaining.Days}d {remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }

    public IRelayCommand LoadCommand => new AsyncRelayCommand(Load);
    public IRelayCommand BackCommand => new AsyncRelayCommand(Back);

    public TimeCapsuleDetailsViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public void PageAppeared()
    {
        if (LoadCommand.CanExecute(null))
            LoadCommand.Execute(null);

        StartCountdown();
    }

    public void PageDisappeared()
    {
        _timer?.Stop();
    }

    private void StartCountdown()
    {
        _timer ??= Application.Current?.Dispatcher.CreateTimer();

        if (_timer == null || _timer.IsRunning)
            return;

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) =>
        {
            RefreshState();

            if (IsUnlocked && !_mediaLoaded && TimeCapsuleId > 0)
                LoadCommand.Execute(null);
        };
        _timer.Start();
    }

    private async Task Load()
    {
        if (TimeCapsuleId <= 0 || IsLoading)
            return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            _capsule = await _apiService.GetAsync<TimeCapsuleResponseDto>($"TimeCapsule/{TimeCapsuleId}");

            if (_capsule == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Capsule could not be loaded."
                    : _apiService.LastErrorMessage;
                return;
            }

            CapsuleTitle = _capsule.Title;
            Description = _capsule.Description ?? string.Empty;
            RefreshState();

            if (IsUnlocked)
                await LoadMedia();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadMedia()
    {
        var result = await _apiService.GetAsync<List<MediaResponseDto>>($"Media/timecapsule/{TimeCapsuleId}");
        MediaItems.Clear();

        if (result == null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                ? "Capsule photos could not be loaded."
                : _apiService.LastErrorMessage;
            return;
        }

        foreach (var item in result)
            MediaItems.Add(new AlbumMediaItemViewModel(item));

        _mediaLoaded = true;
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(IsUnlocked));
        OnPropertyChanged(nameof(IsLocked));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(UnlockDateText));
        OnPropertyChanged(nameof(CountdownText));
    }

    private async Task Back()
    {
        await Shell.Current.GoToAsync("..");
    }
}
