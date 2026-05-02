using Momentix.Data.Models;

namespace Momentix.Data.DTOs;

public class ChallengeResponseDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public ChallengeType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime RevealAt { get; set; }
    public bool IsRevealed { get; set; }
}
