using Momentix.Data.DTOs;
using Microsoft.Maui.Storage;

namespace Momentix.Mobile.ViewModels;

public class FriendItemViewModel
{
    private readonly FriendResponseDto? _friend;

    public bool IsAddCard { get; set; }

    public string UserId => _friend?.UserId ?? string.Empty;
    public string FullName => _friend?.FullName ?? string.Empty;
    public string Email => _friend?.Email ?? string.Empty;
    public string? ProfilePictureUrl => _friend?.ProfilePictureUrl;
    public DateTime AddedAt => _friend?.AddedAt ?? DateTime.MinValue;

    public string Initials
    {
        get
        {
            if (IsAddCard) return string.Empty;
            var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpper();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        }
    }

    public string AvatarColor
    {
        get
        {
            if (IsAddCard) return "#F3F4F6";
            var theme = Preferences.Get("theme_name", "Blue");
            return theme switch
            {
                "Blue" => "#60A5FA",
                "Green" => "#34D399",
                "Yellow" => "#FBBF24",
                "Purple" => "#A78BFA",
                "Black" => "#1F2937",
                _ => "#60A5FA"
            };
        }
    }

    public FriendItemViewModel(FriendResponseDto? friend) => _friend = friend;
}