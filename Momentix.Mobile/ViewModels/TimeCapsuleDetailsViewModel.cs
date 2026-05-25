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
    private bool _isPageVisible;

    public ObservableCollection<AlbumMediaItemViewModel> MediaItems { get; } = new();
    public ObservableCollection<AlbumMemberResponseDto> Members { get; } = new();
    public ObservableCollection<FriendItemViewModel> Friends { get; } = new();

    private int _timeCapsuleId;
    public int TimeCapsuleId
    {
        get => _timeCapsuleId;
        set
        {
            if (_timeCapsuleId == value) return;
            _timeCapsuleId = value;
            _mediaLoaded = false;
            MediaItems.Clear();
            Members.Clear();
            Friends.Clear();
            SelectedFriend = null;
            OnPropertyChanged();
            LoadIfReady();
        }
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

    private FriendItemViewModel? _selectedFriend;
    public FriendItemViewModel? SelectedFriend
    {
        get => _selectedFriend;
        set { _selectedFriend = value; OnPropertyChanged(); }
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

    private bool _isAddMemberPopupVisible;
    public bool IsAddMemberPopupVisible
    {
        get => _isAddMemberPopupVisible;
        set { _isAddMemberPopupVisible = value; OnPropertyChanged(); }
    }

    public bool IsOwner => _capsule?.IsOwner == true;
    public bool IsUnlocked => _capsule != null && (_capsule.IsUnlocked || DateTime.UtcNow >= _capsule.UnlockAt);
    public bool IsLocked => !IsUnlocked;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public string StatusText => IsUnlocked ? "Отключена" : "Заключена";
    public string CountdownCardColor => IsUnlocked ? "#1B4D3E" : "#1A2744";

    public string UnlockDateText => _capsule == null
        ? string.Empty
        : $"Отключва се на {_capsule.UnlockAt.ToLocalTime():dd.MM.yyyy} в {_capsule.UnlockAt.ToLocalTime():HH:mm}";

    public string CountdownText
    {
        get
        {
            if (_capsule == null) return string.Empty;
            if (IsUnlocked) return "Ready to open";
            var remaining = _capsule.UnlockAt - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            if (remaining.TotalDays >= 1)
                return $"{(int)remaining.TotalDays}д {remaining.Hours}ч {remaining.Minutes:00}м";
            return $"{remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }

    public IRelayCommand LoadCommand => new AsyncRelayCommand(Load);
    public IRelayCommand BackCommand => new AsyncRelayCommand(Back);

    public IRelayCommand OpenAddMemberPopupCommand => new RelayCommand(() =>
        IsAddMemberPopupVisible = true);

    public IRelayCommand CloseAddMemberPopupCommand => new RelayCommand(() =>
    {
        IsAddMemberPopupVisible = false;
        StatusMessage = string.Empty;
    });

    public IRelayCommand<FriendItemViewModel> AddSelectedFriendCommand =>
        new AsyncRelayCommand<FriendItemViewModel>(async friend =>
        {
            if (friend == null) return;

            IsLoading = true;
            StatusMessage = string.Empty;

            try
            {
                var success = await _apiService.PostAsync(
                    $"TimeCapsule/{TimeCapsuleId}/members",
                    new AddAlbumMemberDto
                    {
                        UserEmail = friend.Email,
                        CanUpload = false
                    });

                if (!success)
                {
                    StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                        ? "Капсулата не беше споделена."
                        : _apiService.LastErrorMessage;
                    return;
                }

                IsAddMemberPopupVisible = false;
                await LoadMembers();
                await LoadFriends();
                StatusMessage = "Капсулата е споделена.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        });

    public TimeCapsuleDetailsViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public void PageAppeared()
    {
        _isPageVisible = true;
        LoadIfReady();
        StartCountdown();
    }

    public void PageDisappeared()
    {
        _isPageVisible = false;
        _timer?.Stop();
    }

    private void LoadIfReady()
    {
        if (!_isPageVisible || TimeCapsuleId <= 0 || IsLoading)
            return;
        LoadCommand.Execute(null);
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
        if (TimeCapsuleId <= 0 || IsLoading) return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            _capsule = await _apiService.GetAsync<TimeCapsuleResponseDto>($"TimeCapsule/{TimeCapsuleId}");

            if (_capsule == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Капсулата не можа да се зареди."
                    : _apiService.LastErrorMessage;
                return;
            }

            CapsuleTitle = _capsule.Title;
            Description = _capsule.Description ?? string.Empty;
            RefreshState();

            await LoadMembers();

            if (IsOwner)
                await LoadFriends();
            else
                Friends.Clear();

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

    private async Task LoadMembers()
    {
        var result = await _apiService.GetAsync<List<AlbumMemberResponseDto>>($"TimeCapsule/{TimeCapsuleId}/members");
        Members.Clear();
        if (result == null) return;
        foreach (var member in result)
            Members.Add(member);
    }

    private async Task LoadFriends()
    {
        var result = await _apiService.GetAsync<List<FriendResponseDto>>("Friends");
        Friends.Clear();
        if (result == null) return;

        var existingMemberIds = Members.Select(m => m.UserId).ToHashSet();

        foreach (var friend in result
            .GroupBy(f => f.UserId)
            .Select(g => g.First())
            .Where(f => !existingMemberIds.Contains(f.UserId))
            .OrderBy(f => f.FullName))
            Friends.Add(new FriendItemViewModel(friend));

        if (SelectedFriend != null && !Friends.Any(f => f.UserId == SelectedFriend.UserId))
            SelectedFriend = null;
    }

    private async Task LoadMedia()
    {
        var result = await _apiService.GetAsync<List<MediaResponseDto>>($"Media/timecapsule/{TimeCapsuleId}");
        MediaItems.Clear();

        if (result == null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                ? "Снимките не можаха да се заредят."
                : _apiService.LastErrorMessage;
            return;
        }

        foreach (var item in result)
            MediaItems.Add(new AlbumMediaItemViewModel(item));

        _mediaLoaded = true;
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(IsOwner));
        OnPropertyChanged(nameof(IsUnlocked));
        OnPropertyChanged(nameof(IsLocked));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(UnlockDateText));
        OnPropertyChanged(nameof(CountdownText));
        OnPropertyChanged(nameof(HasDescription));
        OnPropertyChanged(nameof(CountdownCardColor));
    }

    private async Task Back()
    {
        await Shell.Current.GoToAsync("..");
    }
}