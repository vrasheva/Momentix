using Momentix.Data.Models;

namespace Momentix.Data.DTOs;

public class FriendRequestResponseDto
{
    public int Id { get; set; }
    public string RequesterId { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string AddresseeId { get; set; } = string.Empty;
    public string AddresseeName { get; set; } = string.Empty;
    public string AddresseeEmail { get; set; } = string.Empty;
    public FriendRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
