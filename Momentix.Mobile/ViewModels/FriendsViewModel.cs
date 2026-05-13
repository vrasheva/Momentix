using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

public partial class FriendsViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    public ObservableCollection<UserSearchItemViewModel> Users { get; } = new();
    public ObservableCollection<FriendRequestItemViewModel> IncomingRequests { get; } = new();
    public ObservableCollection<FriendItemViewModel> Friends { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); }
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

    public IRelayCommand LoadFriendsCommand => new AsyncRelayCommand(LoadAll);
    public IRelayCommand SearchUsersCommand => new AsyncRelayCommand(SearchUsers);
    public IRelayCommand<UserSearchItemViewModel> SendRequestCommand => new AsyncRelayCommand<UserSearchItemViewModel>(SendRequest);
    public IRelayCommand<UserSearchItemViewModel> AcceptSearchRequestCommand => new AsyncRelayCommand<UserSearchItemViewModel>(AcceptSearchRequest);
    public IRelayCommand<FriendRequestItemViewModel> AcceptRequestCommand => new AsyncRelayCommand<FriendRequestItemViewModel>(AcceptIncomingRequest);
    public IRelayCommand<FriendRequestItemViewModel> DeclineRequestCommand => new AsyncRelayCommand<FriendRequestItemViewModel>(DeclineIncomingRequest);
    public IRelayCommand<FriendItemViewModel> RemoveFriendCommand => new AsyncRelayCommand<FriendItemViewModel>(RemoveFriend);

    public FriendsViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private async Task LoadAll()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            await LoadFriends();
            await LoadIncomingRequests();
            await SearchUsers();
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

    private async Task LoadFriends()
    {
        var result = await _apiService.GetAsync<List<FriendResponseDto>>("Friends");
        Friends.Clear();

        if (result == null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                ? "Friends could not be loaded."
                : _apiService.LastErrorMessage;
            return;
        }

        foreach (var friend in result
            .GroupBy(f => f.UserId)
            .Select(g => g.First())
            .OrderBy(f => f.FullName))
            Friends.Add(new FriendItemViewModel(friend));
    }

    private async Task LoadIncomingRequests()
    {
        var result = await _apiService.GetAsync<List<FriendRequestResponseDto>>("Friends/requests/incoming");
        IncomingRequests.Clear();

        if (result == null)
            return;

        foreach (var request in result.OrderByDescending(r => r.CreatedAt))
            IncomingRequests.Add(new FriendRequestItemViewModel(request));
    }

    private async Task SearchUsers()
    {
        var endpoint = string.IsNullOrWhiteSpace(SearchText)
            ? "Friends/users"
            : $"Friends/users?search={Uri.EscapeDataString(SearchText.Trim())}";

        var result = await _apiService.GetAsync<List<UserSearchResponseDto>>(endpoint);
        Users.Clear();

        if (result == null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                ? "Users could not be loaded."
                : _apiService.LastErrorMessage;
            return;
        }

        foreach (var user in result.OrderBy(u => u.FullName))
            Users.Add(new UserSearchItemViewModel(user));
    }

    private async Task SendRequest(UserSearchItemViewModel? user)
    {
        if (user == null)
            return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _apiService.PostAsync<object, FriendRequestResponseDto>(
                $"Friends/requests/{user.UserId}",
                new { });

            if (result == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Friend request was not sent."
                    : _apiService.LastErrorMessage;
                return;
            }

            StatusMessage = "Friend request sent.";
            await LoadFriends();
            await LoadIncomingRequests();
            await SearchUsers();
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

    private async Task AcceptSearchRequest(UserSearchItemViewModel? user)
    {
        if (user?.IncomingRequestId == null)
            return;

        await AcceptRequestById(user.IncomingRequestId.Value);
    }

    private async Task AcceptIncomingRequest(FriendRequestItemViewModel? request)
    {
        if (request == null)
            return;

        await AcceptRequestById(request.Id);
    }

    private async Task AcceptRequestById(int requestId)
    {
        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _apiService.PostAsync<object, FriendRequestResponseDto>(
                $"Friends/requests/{requestId}/accept",
                new { });

            if (result == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Friend request was not accepted."
                    : _apiService.LastErrorMessage;
                return;
            }

            StatusMessage = "Friend request accepted.";
            await LoadFriends();
            await LoadIncomingRequests();
            await SearchUsers();
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

    private async Task DeclineIncomingRequest(FriendRequestItemViewModel? request)
    {
        if (request == null)
            return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _apiService.PostAsync<object, FriendRequestResponseDto>(
                $"Friends/requests/{request.Id}/decline",
                new { });

            if (result == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Friend request was not declined."
                    : _apiService.LastErrorMessage;
                return;
            }

            StatusMessage = "Friend request declined.";
            await LoadIncomingRequests();
            await SearchUsers();
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

    private async Task RemoveFriend(FriendItemViewModel? friend)
    {
        if (friend == null)
            return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var success = await _apiService.DeleteAsync($"Friends/{friend.UserId}");
            if (!success)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Friend was not removed."
                    : _apiService.LastErrorMessage;
                return;
            }

            StatusMessage = "Friend removed.";
            await LoadFriends();
            await SearchUsers();
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
}

public class UserSearchItemViewModel
{
    private readonly UserSearchResponseDto _user;

    public string UserId => _user.UserId;
    public string FullName => _user.FullName;
    public string Email => _user.Email;
    public int? IncomingRequestId => _user.IncomingRequestId;
    public bool CanSendRequest => !_user.IsFriend && !_user.HasPendingOutgoingRequest && !_user.HasPendingIncomingRequest;
    public bool CanAcceptRequest => _user.HasPendingIncomingRequest && _user.IncomingRequestId.HasValue;

    public string StatusText
    {
        get
        {
            if (_user.IsFriend)
                return "Friend";
            if (_user.HasPendingOutgoingRequest)
                return "Request sent";
            if (_user.HasPendingIncomingRequest)
                return "Wants to be friends";
            return "Not connected";
        }
    }

    public UserSearchItemViewModel(UserSearchResponseDto user)
    {
        _user = user;
    }
}

public class FriendRequestItemViewModel
{
    private readonly FriendRequestResponseDto _request;

    public int Id => _request.Id;
    public string RequesterName => _request.RequesterName;
    public string RequesterEmail => _request.RequesterEmail;
    public DateTime CreatedAt => _request.CreatedAt;

    public FriendRequestItemViewModel(FriendRequestResponseDto request)
    {
        _request = request;
    }
}
