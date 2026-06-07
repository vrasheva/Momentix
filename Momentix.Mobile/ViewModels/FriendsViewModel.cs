using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

public partial class FriendsViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    public ObservableCollection<UserSearchItemViewModel> Users { get; } = new();
    public ObservableCollection<UserSearchItemViewModel> Suggestions { get; } = new();
    public ObservableCollection<FriendRequestItemViewModel> IncomingRequests { get; } = new();
    public ObservableCollection<FriendItemViewModel> Friends { get; } = new();

    public bool HasIncomingRequests => IncomingRequests.Count > 0;
    public bool HasSuggestions => Suggestions.Count > 0;
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool HasSearchResult => _searchResultUser != null;
    public bool SearchResultCanAdd => _searchResultUser?.CanSendRequest ?? false;
    public string SearchResultName => _searchResultUser?.FullName ?? string.Empty;
    public string SearchResultInitials => _searchResultUser != null ? AvatarHelper.InitialsFor(_searchResultUser.FullName) : string.Empty;
    public string SearchResultAvatarColor => _searchResultUser != null ? AvatarHelper.ColorFor(_searchResultUser.UserId) : "#888888";
    public string SearchResultStatus => _searchResultUser?.StatusText ?? string.Empty;

    private UserSearchItemViewModel? _searchResultUser;
    private bool _isSearchPopupVisible;

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
        set { _statusMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStatusMessage)); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public bool IsSearchPopupVisible
    {
        get => _isSearchPopupVisible;
        set { _isSearchPopupVisible = value; OnPropertyChanged(); }
    }

    public IRelayCommand LoadFriendsCommand => new AsyncRelayCommand(LoadAll);
    public IRelayCommand SearchUsersCommand => new AsyncRelayCommand(SearchUsers);
    public IRelayCommand SendRequestToSearchResultCommand => new AsyncRelayCommand(SendRequestToSearchResult);
    public IRelayCommand CloseSearchPopupCommand => new RelayCommand(CloseSearchPopup);
    public IRelayCommand<FriendItemViewModel> CardTappedCommand => new RelayCommand<FriendItemViewModel>(OnCardTapped);
    public IRelayCommand<UserSearchItemViewModel> SendRequestCommand => new AsyncRelayCommand<UserSearchItemViewModel>(SendRequest);
    public IRelayCommand<UserSearchItemViewModel> AcceptSearchRequestCommand => new AsyncRelayCommand<UserSearchItemViewModel>(AcceptSearchRequest);
    public IRelayCommand<FriendRequestItemViewModel> AcceptRequestCommand => new AsyncRelayCommand<FriendRequestItemViewModel>(AcceptIncomingRequest);
    public IRelayCommand<FriendRequestItemViewModel> DeclineRequestCommand => new AsyncRelayCommand<FriendRequestItemViewModel>(DeclineIncomingRequest);
    public IRelayCommand<FriendItemViewModel> RemoveFriendCommand => new AsyncRelayCommand<FriendItemViewModel>(RemoveFriend);
    public IRelayCommand GoToProfileCommand => new AsyncRelayCommand(async () =>
        await Shell.Current.GoToAsync("ProfilePage"));
    public IRelayCommand GoToNotificationsCommand => new AsyncRelayCommand(async () =>
        await Shell.Current.GoToAsync("NotificationsPage"));

    public FriendsViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private void OnCardTapped(FriendItemViewModel? card)
    {
        if (card?.IsAddCard == true)
            IsSearchPopupVisible = true;
    }

    private void CloseSearchPopup()
    {
        IsSearchPopupVisible = false;
        SearchText = string.Empty;
        _searchResultUser = null;
        NotifySearchResult();
    }

    private void NotifySearchResult()
    {
        OnPropertyChanged(nameof(HasSearchResult));
        OnPropertyChanged(nameof(SearchResultName));
        OnPropertyChanged(nameof(SearchResultCanAdd));
        OnPropertyChanged(nameof(SearchResultInitials));
        OnPropertyChanged(nameof(SearchResultAvatarColor));
        OnPropertyChanged(nameof(SearchResultStatus));
    }

    private async Task LoadAll()
    {
        if (IsLoading) return;
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await LoadFriends();
            await LoadIncomingRequests();
            await SearchUsers();
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    private async Task LoadFriends()
    {
        var result = await _apiService.GetAsync<List<FriendResponseDto>>("Friends");
        Friends.Clear();
        Friends.Add(new FriendItemViewModel(null) { IsAddCard = true });

        if (result == null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                ? "Приятелите не можаха да се заредят."
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
        if (result == null) return;
        foreach (var request in result.OrderByDescending(r => r.CreatedAt))
            IncomingRequests.Add(new FriendRequestItemViewModel(request));
        OnPropertyChanged(nameof(HasIncomingRequests));
    }

    private async Task SearchUsers()
    {
        var endpoint = string.IsNullOrWhiteSpace(SearchText)
            ? "Friends/users"
            : $"Friends/users?search={Uri.EscapeDataString(SearchText.Trim())}";

        var result = await _apiService.GetAsync<List<UserSearchResponseDto>>(endpoint);
        Users.Clear();
        Suggestions.Clear();
        _searchResultUser = null;

        if (result == null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                ? "Потребителите не можаха да се заредят."
                : _apiService.LastErrorMessage;
            OnPropertyChanged(nameof(HasSuggestions));
            NotifySearchResult();
            return;
        }

        foreach (var user in result.OrderBy(u => u.FullName))
        {
            var vm = new UserSearchItemViewModel(user);
            Users.Add(vm);
            if (!user.IsFriend && !user.HasPendingOutgoingRequest)
                Suggestions.Add(vm);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            _searchResultUser = result
                .Select(u => new UserSearchItemViewModel(u))
                .FirstOrDefault(u =>
                    u.Email.Equals(SearchText.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    u.FullName.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        OnPropertyChanged(nameof(HasSuggestions));
        NotifySearchResult();
    }

    private async Task SendRequest(UserSearchItemViewModel? user)
    {
        if (user == null) return;
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _apiService.PostAsync<object, FriendRequestResponseDto>(
                $"Friends/requests/{user.UserId}", new { });
            if (result == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Заявката не беше изпратена."
                    : _apiService.LastErrorMessage;
                return;
            }
            StatusMessage = "Заявката е изпратена.";
            await LoadAll();
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    private async Task SendRequestToSearchResult()
    {
        if (_searchResultUser == null) return;
        await SendRequest(_searchResultUser);
        IsSearchPopupVisible = false;
        SearchText = string.Empty;
        _searchResultUser = null;
        NotifySearchResult();
    }

    private async Task AcceptSearchRequest(UserSearchItemViewModel? user)
    {
        if (user?.IncomingRequestId == null) return;
        await AcceptRequestById(user.IncomingRequestId.Value);
    }

    private async Task AcceptIncomingRequest(FriendRequestItemViewModel? request)
    {
        if (request == null) return;
        await AcceptRequestById(request.Id);
    }

    private async Task AcceptRequestById(int requestId)
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _apiService.PostAsync<object, FriendRequestResponseDto>(
                $"Friends/requests/{requestId}/accept", new { });
            if (result == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Заявката не беше приета."
                    : _apiService.LastErrorMessage;
                return;
            }
            StatusMessage = "Заявката е приета.";
            await LoadAll();
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    private async Task DeclineIncomingRequest(FriendRequestItemViewModel? request)
    {
        if (request == null) return;
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _apiService.PostAsync<object, FriendRequestResponseDto>(
                $"Friends/requests/{request.Id}/decline", new { });
            if (result == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Заявката не беше отхвърлена."
                    : _apiService.LastErrorMessage;
                return;
            }
            StatusMessage = "Заявката е отхвърлена.";
            await LoadIncomingRequests();
            await SearchUsers();
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    private async Task RemoveFriend(FriendItemViewModel? friend)
    {
        if (friend == null || friend.IsAddCard) return;
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var success = await _apiService.DeleteAsync($"Friends/{friend.UserId}");
            if (!success)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Приятелят не беше премахнат."
                    : _apiService.LastErrorMessage;
                return;
            }
            StatusMessage = "Приятелят е премахнат.";
            await LoadFriends();
            await SearchUsers();
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsLoading = false; }
    }
}

static file class AvatarHelper
{
    private static readonly string[] Colors =
    [
        "#6750A4", "#7B61C4", "#4A90D9", "#43A047",
        "#E67E22", "#E91E63", "#00ACC1", "#8D6E63"
    ];

    public static string ColorFor(string seed)
    {
        var index = Math.Abs(seed.GetHashCode()) % Colors.Length;
        return Colors[index];
    }

    public static string InitialsFor(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpper();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
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
    public string Initials => AvatarHelper.InitialsFor(_user.FullName);
    public string AvatarColor => AvatarHelper.ColorFor(_user.UserId);

    public string StatusText
    {
        get
        {
            if (_user.IsFriend) return "Приятел";
            if (_user.HasPendingOutgoingRequest) return "Заявката е изпратена";
            if (_user.HasPendingIncomingRequest) return "Иска да бъде приятел";
            return "Не сте свързани";
        }
    }

    public UserSearchItemViewModel(UserSearchResponseDto user) => _user = user;
}

public class FriendRequestItemViewModel
{
    private readonly FriendRequestResponseDto _request;

    public int Id => _request.Id;
    public string RequesterName => _request.RequesterName;
    public string RequesterEmail => _request.RequesterEmail;
    public DateTime CreatedAt => _request.CreatedAt;
    public string Initials => AvatarHelper.InitialsFor(_request.RequesterName);
    public string AvatarColor => AvatarHelper.ColorFor(_request.RequesterId);

    public FriendRequestItemViewModel(FriendRequestResponseDto request) => _request = request;
}