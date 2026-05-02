namespace Momentix.Data.DTOs;

public class ChallengeSubmissionResponseDto
{
    public int Id { get; set; }
    public int ChallengeId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int VoteCount { get; set; }
}
