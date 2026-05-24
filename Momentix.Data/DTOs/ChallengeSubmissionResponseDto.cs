namespace Momentix.Data.DTOs;

public class ChallengeSubmissionResponseDto
{
    public int Id { get; set; }
    public int ChallengeId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int VoteCount { get; set; }
    public bool? AiIsSatisfied { get; set; }
    public int? AiConfidence { get; set; }
    public string? AiFeedback { get; set; }
    public string? AiModel { get; set; }
    public DateTime? AiEvaluatedAt { get; set; }
}
