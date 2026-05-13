namespace Momentix.Data.Models;

public enum FriendRequestStatus
{
    Pending,
    Accepted,
    Declined
}

public class FriendRequest
{
    public int Id { get; set; }

    public string RequesterId { get; set; } = string.Empty;
    public User Requester { get; set; } = null!;

    public string AddresseeId { get; set; } = string.Empty;
    public User Addressee { get; set; } = null!;

    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}
