using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

public partial class NotificationsViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    public ObservableCollection<NotificationResponseDto> Notifications { get; } = new();

    private int _unreadCount;
    public int UnreadCount
    {
        get => _unreadCount;
        set
        {
            _unreadCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HeaderText));
        }
    }

    public string HeaderText => UnreadCount == 0
        ? "All notifications are read"
        : $"{UnreadCount} unread notifications";

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

    public IRelayCommand LoadCommand => new AsyncRelayCommand(Load);
    public IRelayCommand MarkAllAsReadCommand => new AsyncRelayCommand(MarkAllAsRead);
    public IRelayCommand<NotificationResponseDto> OpenNotificationCommand => new AsyncRelayCommand<NotificationResponseDto>(OpenNotification);

    public NotificationsViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private async Task Load()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var notifications = await _apiService.GetAsync<List<NotificationResponseDto>>("Notifications") ?? new();
            var unread = await _apiService.GetAsync<NotificationUnreadCountDto>("Notifications/unread-count");

            Notifications.Clear();
            foreach (var notification in notifications)
                Notifications.Add(notification);

            UnreadCount = unread?.Count ?? notifications.Count(n => !n.IsRead);
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

    private async Task MarkAllAsRead()
    {
        var success = await _apiService.PostAsync("Notifications/read-all");
        StatusMessage = success ? "Notifications marked as read." : _apiService.LastErrorMessage;
        await Load();
    }

    private async Task OpenNotification(NotificationResponseDto? notification)
    {
        if (notification == null)
            return;

        if (!notification.IsRead)
            await _apiService.PostAsync($"Notifications/{notification.Id}/read");

        await NavigateToRelated(notification);
        await Load();
    }

    private static async Task NavigateToRelated(NotificationResponseDto notification)
    {
        if (notification.RelatedEntityId.HasValue && notification.RelatedEntityType == "Album")
        {
            await Shell.Current.GoToAsync($"AlbumDetailsPage?AlbumId={notification.RelatedEntityId.Value}");
            return;
        }

        if (notification.RelatedEntityId.HasValue && notification.RelatedEntityType == "TimeCapsule")
        {
            await Shell.Current.GoToAsync($"TimeCapsuleDetailsPage?TimeCapsuleId={notification.RelatedEntityId.Value}");
            return;
        }

        if (notification.RelatedEntityType == "FriendRequest" || notification.RelatedEntityType == "Friend")
        {
            await Shell.Current.GoToAsync("//FriendsPage");
            return;
        }

        if (notification.RelatedEntityType == "ChallengeSubmission")
        {
            await Shell.Current.GoToAsync("//ChallengesPage");
            return;
        }
    }
}
