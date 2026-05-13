namespace Momentix.Data.DTOs;

public class UserSearchResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsFriend { get; set; }
    public bool HasPendingOutgoingRequest { get; set; }
    public bool HasPendingIncomingRequest { get; set; }
    public int? IncomingRequestId { get; set; }
}
