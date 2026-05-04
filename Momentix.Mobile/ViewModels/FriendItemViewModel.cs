using Momentix.Data.DTOs;

namespace Momentix.Mobile.ViewModels;

public class FriendItemViewModel
{
    private readonly FriendResponseDto _friend;

    public string UserId => _friend.UserId;
    public string FullName => _friend.FullName;
    public string Email => _friend.Email;
    public string? ProfilePictureUrl => _friend.ProfilePictureUrl;
    public DateTime AddedAt => _friend.AddedAt;
    public string DisplayName => string.IsNullOrWhiteSpace(FullName)
        ? Email
        : $"{FullName} ({Email})";

    public FriendItemViewModel(FriendResponseDto friend)
    {
        _friend = friend;
    }
}
