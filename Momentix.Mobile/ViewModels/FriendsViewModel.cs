using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

public partial class FriendsViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    public ObservableCollection<FriendItemViewModel> Friends { get; } = new();

    private string _friendEmail = string.Empty;
    public string FriendEmail
    {
        get => _friendEmail;
        set { _friendEmail = value; OnPropertyChanged(); }
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

    public IRelayCommand LoadFriendsCommand => new AsyncRelayCommand(LoadFriends);
    public IRelayCommand AddFriendCommand => new AsyncRelayCommand(AddFriend);
    public IRelayCommand<FriendItemViewModel> RemoveFriendCommand => new AsyncRelayCommand<FriendItemViewModel>(RemoveFriend);

    public FriendsViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private async Task LoadFriends()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
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
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddFriend()
    {
        if (string.IsNullOrWhiteSpace(FriendEmail))
        {
            StatusMessage = "Email is required.";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _apiService.PostAsync<AddFriendDto, FriendResponseDto>(
                "Friends",
                new AddFriendDto { Email = FriendEmail.Trim() });

            if (result == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Friend was not added."
                    : _apiService.LastErrorMessage;
                return;
            }

            if (!Friends.Any(f => f.UserId == result.UserId))
                Friends.Add(new FriendItemViewModel(result));

            FriendEmail = string.Empty;
            StatusMessage = "Friend added.";
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

            Friends.Remove(friend);
            StatusMessage = "Friend removed.";
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
