namespace Momentix.Data.DTOs;

public class AlbumMemberResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool CanUpload { get; set; }
    public bool IsOwner { get; set; }

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FullName)) return "?";
            var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        }
    }

    public string AvatarColor
    {
        get
        {
            if (string.IsNullOrWhiteSpace(UserId)) return "#888888";
            var colors = new[]
            {
                "#60A5FA", "#34D399", "#FBBF24",
                "#A78BFA", "#F87171", "#38BDF8",
                "#4ADE80", "#FB923C"
            };
            var index = Math.Abs(UserId.GetHashCode()) % colors.Length;
            return colors[index];
        }
    }

    public string RoleText => IsOwner ? "Собственик" :
                              CanUpload ? "Може да качва" : "Само преглед";
}