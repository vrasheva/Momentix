using Momentix.Data.DTOs;

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

    public string CardBackground => IsAddCard ? "#F3F4F6" : AvatarColor;
    public string CardStroke => IsAddCard ? "#E5E3DC" : "Transparent";

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
            string[] colors = ["#6750A4", "#7B61C4", "#4A90D9", "#43A047",
                               "#E67E22", "#E91E63", "#00ACC1", "#8D6E63"];
            var index = Math.Abs(UserId.GetHashCode()) % colors.Length;
            return colors[index];
        }
    }

    public FriendItemViewModel(FriendResponseDto? friend) => _friend = friend;
}