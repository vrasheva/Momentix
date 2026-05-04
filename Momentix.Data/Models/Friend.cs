namespace Momentix.Data.Models;

public class Friend
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public string FriendUserId { get; set; } = string.Empty;
    public User FriendUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
