namespace Momentix.Data.Models;

public enum NotificationType
{
    FriendRequest = 0,
    FriendAccepted = 1,
    AlbumShared = 2,
    CapsuleShared = 3,
    ChallengeSubmission = 4,
    ChallengeReveal = 5,
    CapsuleUnlocked = 6
}

public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public string? ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
